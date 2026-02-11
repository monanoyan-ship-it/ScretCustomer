"""Extract all unique Turkish strings from JS files."""
import sys, io, os, re
from collections import defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

base = r'C:\Users\Ahmet\source\repos\monanoyan-ship-it\ScretCustomer\Backend\SecretCustomer.API\wwwroot\js'
TR_CHARS = 'şçöüğıİŞÇÖÜĞ'
str_pattern = re.compile(r"""(['"])([^'"]*?[""" + TR_CHARS + r""""][^'"]*?)\1""")

all_strings = defaultdict(list)

for root, dirs, files in os.walk(base):
    dirs[:] = [d for d in dirs if d not in ('lib', 'node_modules')]
    for f in files:
        if not f.endswith('.js'):
            continue
        fpath = os.path.join(root, f)
        try:
            with open(fpath, 'r', encoding='utf-8-sig') as fh:
                content = fh.read()
        except:
            continue
        lines = content.split('\n')
        for i, line in enumerate(lines, 1):
            stripped = line.strip()
            if not stripped or stripped.startswith('//') or stripped.startswith('/*') or stripped.startswith('*'):
                continue
            for match in str_pattern.finditer(line):
                text = match.group(2)
                pos = match.start()
                before = line[:pos]
                # Skip if inside T() call (fallback param)
                if re.search(r"T\(\s*['\"][^'\"]*['\"]\s*,\s*$", before):
                    continue
                # Skip if key param of T()
                if re.search(r"T\(\s*$", before.rstrip()):
                    continue
                rel = os.path.relpath(fpath, base)
                all_strings[text].append((rel, i))
                break

for text in sorted(all_strings.keys()):
    locs = all_strings[text]
    files = sorted(set(loc[0] for loc in locs))
    print(f'{text} ||| {len(locs)} ||| {";".join(files)}')
