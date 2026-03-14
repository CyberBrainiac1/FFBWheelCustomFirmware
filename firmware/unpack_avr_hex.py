#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path


PRINTABLE_RE = re.compile(rb"[\x20-\x7e]{4,}")
UTF16LE_RE = re.compile(rb"(?:[\x20-\x7e]\x00){4,}")


def find_tool(name: str) -> Path:
    exe_names = [name]
    if os.name == "nt":
        exe_names.insert(0, f"{name}.exe")

    for exe_name in exe_names:
        found = shutil.which(exe_name)
        if found:
            return Path(found)

    roots = []
    localappdata = os.environ.get("LOCALAPPDATA")
    if localappdata:
        roots.append(Path(localappdata) / "Arduino15" / "packages")
    roots.extend(
        [
            Path.home() / ".arduino15" / "packages",
            Path.home() / "Library" / "Arduino15" / "packages",
        ]
    )

    seen = set()
    for root in roots:
        if not root.exists():
            continue
        for candidate in root.rglob("*"):
            if candidate.name not in exe_names or not candidate.is_file():
                continue
            resolved = candidate.resolve()
            if resolved in seen:
                continue
            seen.add(resolved)
            return resolved

    raise FileNotFoundError(f"Unable to find {name} in PATH or Arduino tool directories")


def run_capture(command: list[str]) -> str:
    completed = subprocess.run(command, check=True, capture_output=True, text=True)
    return completed.stdout


def run_no_capture(command: list[str]) -> None:
    subprocess.run(command, check=True)


def write_text(path: Path, contents: str) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write(contents)


def extract_ascii_strings(data: bytes) -> list[tuple[int, str]]:
    return [(match.start(), match.group().decode("ascii")) for match in PRINTABLE_RE.finditer(data)]


def extract_utf16le_strings(data: bytes) -> list[tuple[int, str]]:
    strings = []
    for match in UTF16LE_RE.finditer(data):
        text = match.group().decode("utf-16le")
        strings.append((match.start(), text))
    return strings


def find_usb_device_descriptor(data: bytes) -> dict[str, int] | None:
    for offset in range(0, len(data) - 18):
        chunk = data[offset : offset + 18]
        if chunk[0] != 0x12 or chunk[1] != 0x01:
            continue
        vendor_id = chunk[8] | (chunk[9] << 8)
        product_id = chunk[10] | (chunk[11] << 8)
        bcd_device = chunk[12] | (chunk[13] << 8)
        return {
            "offset": offset,
            "bcd_usb": chunk[2] | (chunk[3] << 8),
            "device_class": chunk[4],
            "device_subclass": chunk[5],
            "device_protocol": chunk[6],
            "max_packet_size_0": chunk[7],
            "vendor_id": vendor_id,
            "product_id": product_id,
            "bcd_device": bcd_device,
            "manufacturer_index": chunk[14],
            "product_index": chunk[15],
            "serial_index": chunk[16],
            "num_configurations": chunk[17],
        }
    return None


def describe_usb_class(device_class: int, device_subclass: int, device_protocol: int) -> str | None:
    if (device_class, device_subclass, device_protocol) == (0xEF, 0x02, 0x01):
        return "Miscellaneous / Common / Interface Association (composite USB device)"
    return None


def build_summary(
    input_hex: Path,
    binary_path: Path,
    elf_path: Path,
    arch: str,
    ascii_strings: list[tuple[int, str]],
    utf16_strings: list[tuple[int, str]],
    usb_descriptor: dict[str, int] | None,
    artifacts: list[Path],
) -> str:
    binary_size = binary_path.stat().st_size
    lines = [
        f"Input HEX: {input_hex.name}",
        f"Binary image: {binary_path.name} ({binary_size} bytes / 0x{binary_size:x})",
        f"ELF wrapper: {elf_path.name}",
        f"Decode architecture: {arch} (chosen for disassembly; Intel HEX does not encode the exact MCU model)",
        "",
    ]

    if usb_descriptor:
        usb_class_text = describe_usb_class(
            usb_descriptor["device_class"],
            usb_descriptor["device_subclass"],
            usb_descriptor["device_protocol"],
        )
        lines.extend(
            [
                "USB device descriptor:",
                f"  Offset: 0x{usb_descriptor['offset']:x}",
                f"  bcdUSB: 0x{usb_descriptor['bcd_usb']:04x}",
                f"  Class/Subclass/Protocol: 0x{usb_descriptor['device_class']:02x}/0x{usb_descriptor['device_subclass']:02x}/0x{usb_descriptor['device_protocol']:02x}",
                *( [f"  Class meaning: {usb_class_text}"] if usb_class_text else [] ),
                f"  EP0 max packet size: {usb_descriptor['max_packet_size_0']}",
                f"  VID:PID: 0x{usb_descriptor['vendor_id']:04x}:0x{usb_descriptor['product_id']:04x}",
                f"  Device release: 0x{usb_descriptor['bcd_device']:04x}",
                f"  String indexes: mfg={usb_descriptor['manufacturer_index']} product={usb_descriptor['product_index']} serial={usb_descriptor['serial_index']}",
                f"  Configurations: {usb_descriptor['num_configurations']}",
                "",
            ]
        )

    if ascii_strings:
        lines.append("Sample ASCII strings:")
        for offset, text in ascii_strings[:12]:
            lines.append(f"  0x{offset:04x}: {text}")
        lines.append("")

    if utf16_strings:
        lines.append("Sample UTF-16LE strings:")
        for offset, text in utf16_strings[:12]:
            lines.append(f"  0x{offset:04x}: {text}")
        lines.append("")

    lines.append("Artifacts:")
    for artifact in artifacts:
        lines.append(f"  {artifact.name}")

    return "\n".join(lines) + "\n"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Unpack an AVR Intel HEX image into reverse-engineering artifacts.")
    parser.add_argument("input_hex", type=Path, help="Path to the Intel HEX firmware image")
    parser.add_argument(
        "-o",
        "--output-dir",
        type=Path,
        default=None,
        help="Directory to write unpacked artifacts into (defaults to <hex-stem>_unpack)",
    )
    parser.add_argument(
        "--arch",
        default="avr5",
        help="Architecture passed to avr-objdump for decoding (default: avr5)",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    input_hex = args.input_hex.resolve()
    if not input_hex.is_file():
        print(f"Input file not found: {input_hex}", file=sys.stderr)
        return 1

    output_dir = args.output_dir.resolve() if args.output_dir else input_hex.with_name(f"{input_hex.stem}_unpack")
    output_dir.mkdir(parents=True, exist_ok=True)

    objcopy = find_tool("avr-objcopy")
    objdump = find_tool("avr-objdump")
    readelf = find_tool("avr-readelf")
    size = find_tool("avr-size")

    base_name = input_hex.stem
    binary_path = output_dir / f"{base_name}.bin"
    elf_path = output_dir / f"{base_name}.elf"
    readelf_path = output_dir / f"{base_name}.readelf.txt"
    sections_path = output_dir / f"{base_name}.size.txt"
    disasm_path = output_dir / f"{base_name}.{args.arch}.disasm.txt"
    strings_path = output_dir / f"{base_name}.strings.txt"
    summary_path = output_dir / "summary.txt"

    run_no_capture([str(objcopy), "-I", "ihex", "-O", "binary", str(input_hex), str(binary_path)])
    run_no_capture([str(objcopy), "-I", "ihex", "-O", "elf32-avr", "-B", "avr", str(input_hex), str(elf_path)])

    readelf_text = run_capture([str(readelf), "-h", "-S", str(elf_path)])
    write_text(readelf_path, readelf_text)

    size_text = run_capture([str(size), "-A", "-x", str(elf_path)])
    write_text(sections_path, size_text)

    disasm_text = run_capture([str(objdump), "-b", "ihex", "-m", args.arch, "-D", str(input_hex)])
    write_text(disasm_path, disasm_text)

    data = binary_path.read_bytes()
    ascii_strings = extract_ascii_strings(data)
    utf16_strings = extract_utf16le_strings(data)

    combined_strings = []
    for offset, text in ascii_strings:
        combined_strings.append((offset, "ascii", text))
    for offset, text in utf16_strings:
        combined_strings.append((offset, "utf16le", text))
    combined_strings.sort(key=lambda item: (item[0], item[1], item[2]))

    seen = set()
    string_lines = []
    for offset, string_type, text in combined_strings:
        key = (offset, string_type, text)
        if key in seen:
            continue
        seen.add(key)
        string_lines.append(f"0x{offset:04x} [{string_type}] {text}")
    write_text(strings_path, "\n".join(string_lines) + ("\n" if string_lines else ""))

    usb_descriptor = find_usb_device_descriptor(data)
    artifacts = [binary_path, elf_path, readelf_path, sections_path, disasm_path, strings_path]
    summary_text = build_summary(
        input_hex=input_hex,
        binary_path=binary_path,
        elf_path=elf_path,
        arch=args.arch,
        ascii_strings=ascii_strings,
        utf16_strings=utf16_strings,
        usb_descriptor=usb_descriptor,
        artifacts=artifacts,
    )
    write_text(summary_path, summary_text)

    print(f"Wrote unpacked artifacts to {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
