#!/usr/bin/env python3
"""Add missing localization keys to all 4 XML files"""
import re, os, sys
sys.stdout.reconfigure(encoding='utf-8')

base = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
views_dir = os.path.join(base, 'Backend', 'SecretCustomer.API', 'Views')
loc_dir = os.path.join(base, 'Backend', 'SecretCustomer.API', 'App_Data', 'Localization')

# Extract all keys from views
view_keys = {}
for root, dirs, files in os.walk(views_dir):
    for f in files:
        if f.endswith('.cshtml'):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as fh:
                content = fh.read()
            matches = re.findall(r'Html\.T\("([^"]+)"\s*,\s*"([^"]*?)"', content)
            for key, fallback in matches:
                if key not in view_keys:
                    view_keys[key] = fallback

# Process each XML file
xml_files = {
    'resources.tr.xml': None,  # Use fallback text as-is (Turkish)
    'resources.en.xml': None,
    'resources.de.xml': None,
    'resources.es.xml': None,
}

for xml_name in xml_files:
    xml_path = os.path.join(loc_dir, xml_name)
    with open(xml_path, 'r', encoding='utf-8') as fh:
        xml_content = fh.read()

    # Extract existing keys
    existing_keys = set(re.findall(r'Name="([^"]+)"', xml_content))

    # Find missing keys
    missing = {}
    for key, fallback in sorted(view_keys.items()):
        if key not in existing_keys:
            missing[key] = fallback

    if not missing:
        print(f'{xml_name}: No missing keys')
        continue

    # Generate XML entries for missing keys
    entries = []
    for key, value in sorted(missing.items()):
        # Escape XML special characters in value
        safe_value = value.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;')
        entries.append(f'  <LocaleResource Name="{key}">\n    <Value>{safe_value}</Value>\n  </LocaleResource>')

    new_block = '\n'.join(entries)

    # Insert before closing </Language> tag
    xml_content = xml_content.replace('</Language>', f'{new_block}\n</Language>')

    with open(xml_path, 'w', encoding='utf-8') as fh:
        fh.write(xml_content)

    print(f'{xml_name}: Added {len(missing)} keys')

print('\nDone!')
