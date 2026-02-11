"""
Hardcoded Turkce metinleri tespit eden script.
T() veya Html.T() ile sarilmamis, kullaniciya gorunen Turkce string'leri bulur.
Yorumlar, teknik string'ler ve false positive'ler filtrelenir.
"""
import sys, io, os, re, json
from collections import defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

base_proj = r'C:\Users\Ahmet\source\repos\monanoyan-ship-it\ScretCustomer\Backend'

TR_CHARS = 'şçöüğıİŞÇÖÜĞ'
TR_PATTERN = re.compile(f'[{TR_CHARS}]')

# ---- CSHTML SCANNING ----

def scan_cshtml(content, fpath):
    findings = []
    lines = content.split('\n')

    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        if not stripped:
            continue

        # SKIP: HTML comments
        if stripped.startswith('<!--') or stripped.startswith('-->') or stripped.startswith('@*') or stripped.startswith('*@'):
            continue
        if '<!--' in stripped and '-->' in stripped:
            # Remove HTML comments and check remaining
            no_comments = re.sub(r'<!--.*?-->', '', stripped)
            if not TR_PATTERN.search(no_comments):
                continue
            stripped = no_comments

        # SKIP: Razor directives
        if stripped.startswith('@using') or stripped.startswith('@model') or stripped.startswith('@inject') or stripped.startswith('@section') or stripped.startswith('@addTagHelper'):
            continue
        # SKIP: Pure C# blocks
        if stripped.startswith('@{') or stripped == '{' or stripped == '}' or stripped.startswith('var ') or stripped.startswith('if ') or stripped.startswith('else') or stripped.startswith('foreach') or stripped.startswith('for ('):
            continue
        # SKIP: ViewBag/ViewData
        if 'ViewBag.' in stripped or 'ViewData[' in stripped:
            continue
        # SKIP: C# comments in razor
        if stripped.startswith('//'):
            continue

        # Remove all Html.T() calls from the line to check remaining text
        cleaned = re.sub(r'@?Html\.T\([^)]+\)', '', stripped)

        # Check for Turkish chars in remaining text
        if not TR_PATTERN.search(cleaned):
            continue

        # SKIP: Lines where Turkish text is ONLY in data-bind expressions (those are JS, handled separately)
        # But data-bind with inline Turkish text IS a finding
        # SKIP: Lines where Turkish is only in CSS class names or technical attrs
        if re.match(r'^<[^>]+>$', cleaned) and 'placeholder=' not in cleaned.lower() and 'title=' not in cleaned.lower():
            # Tag-only line without user-visible Turkish attrs
            # But check for Turkish in tag text
            tag_text = re.sub(r'<[^>]*>', '', cleaned).strip()
            if not TR_PATTERN.search(tag_text):
                continue

        # Categorize the finding
        category = 'text'
        if 'placeholder=' in stripped.lower() and TR_PATTERN.search(stripped.split('placeholder')[1][:60] if 'placeholder' in stripped else ''):
            category = 'placeholder'
        elif 'title=' in stripped.lower() and TR_PATTERN.search(stripped.split('title')[1][:60] if 'title' in stripped else ''):
            category = 'title-attr'
        elif 'data-bind="' in stripped and TR_PATTERN.search(stripped):
            category = 'data-bind'

        findings.append((i, category, stripped[:150]))

    return findings


# ---- JS SCANNING ----

def scan_js(content, fpath):
    findings = []
    lines = content.split('\n')

    # Turkish string in quotes
    str_pattern = re.compile(r"""(['"`])([^'"`]*?[""" + TR_CHARS + r"""][^'"`]*?)\1""")
    # T() call pattern
    t_call_pattern = re.compile(r"""T\(\s*['"]([^'"]+)['"]\s*,\s*['"]([^'"]+)['"]""")

    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        if not stripped or stripped.startswith('//') or stripped.startswith('/*') or stripped.startswith('*'):
            continue

        # Find Turkish strings
        for match in str_pattern.finditer(line):
            text = match.group(2)
            pos = match.start()

            # Check if inside T() call (as the 2nd param = fallback, which is OK)
            is_t_fallback = False
            t_matches = list(t_call_pattern.finditer(line))
            for tm in t_matches:
                if tm.start() <= pos <= tm.end():
                    is_t_fallback = True
                    break

            # Also check: T('Key', 'THIS')
            before = line[:pos]
            if re.search(r"T\(\s*['\"][^'\"]*['\"]\s*,\s*$", before):
                is_t_fallback = True

            # Check T('THIS') - key-only call (still localized)
            if re.search(r"T\(\s*$", before.rstrip()):
                is_t_fallback = True

            if not is_t_fallback:
                findings.append((i, 'string', stripped[:150]))
                break

    return findings


# ---- CS SCANNING ----

def scan_cs(content, fpath):
    findings = []
    lines = content.split('\n')

    str_pattern = re.compile(r'"([^"]*[' + TR_CHARS + r'][^"]*)"')

    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        if not stripped or stripped.startswith('//') or stripped.startswith('/*') or stripped.startswith('*') or stripped.startswith('///'):
            continue

        # Remove T() calls
        cleaned = re.sub(r'T\("[^"]*"\s*(?:,\s*"[^"]*")?\)', '', line)

        for match in str_pattern.finditer(cleaned):
            text = match.group(1)
            if len(text) < 2:
                continue
            # Skip namespaces/paths
            if '.' in text and ' ' not in text and '/' not in text:
                continue
            if text.startswith('/') or text.startswith('http') or text.startswith('api/'):
                continue
            # Skip format strings that are just placeholders
            if text.startswith('{') and text.endswith('}'):
                continue
            # Skip migration/SQL
            if 'migration' in fpath.lower() or 'Migration' in fpath:
                continue

            findings.append((i, 'string', stripped[:150]))
            break

    return findings


# ---- MAIN ----

extensions_scanners = {
    '.cshtml': scan_cshtml,
    '.js': scan_js,
    '.cs': scan_cs,
}

source_dirs = [
    os.path.join(base_proj, 'SecretCustomer.API'),
    os.path.join(base_proj, 'SecretCustomer.Services'),
    os.path.join(base_proj, 'SecretCustomer.Core'),
]

source_files = []
for d in source_dirs:
    for root_dir, dirs, files in os.walk(d):
        dirs[:] = [dd for dd in dirs if dd not in ('node_modules', 'bin', 'obj', 'lib', '.git', 'publish')]
        for f in files:
            ext = os.path.splitext(f)[1]
            if ext in extensions_scanners:
                source_files.append(os.path.join(root_dir, f))

print(f"Scanning {len(source_files)} files...\n")

prefix = base_proj + os.sep
all_findings = defaultdict(list)
total = 0

for fpath in source_files:
    ext = os.path.splitext(fpath)[1]
    scanner = extensions_scanners[ext]
    try:
        with open(fpath, 'r', encoding='utf-8-sig') as f:
            content = f.read()
    except:
        continue

    findings = scanner(content, fpath)
    if findings:
        short = fpath.replace(prefix, '') if fpath.startswith(prefix) else fpath
        all_findings[short] = findings
        total += len(findings)

cshtml_files = {k: v for k, v in all_findings.items() if k.endswith('.cshtml')}
js_files = {k: v for k, v in all_findings.items() if k.endswith('.js')}
cs_files = {k: v for k, v in all_findings.items() if k.endswith('.cs')}

print(f"=== OZET ===")
print(f"Toplam hardcoded Turkce: {total} satir")
print(f"  .cshtml: {sum(len(v) for v in cshtml_files.values())} ({len(cshtml_files)} dosya)")
print(f"  .js: {sum(len(v) for v in js_files.values())} ({len(js_files)} dosya)")
print(f"  .cs: {sum(len(v) for v in cs_files.values())} ({len(cs_files)} dosya)")

for label, group in [("CSHTML", cshtml_files), ("JS", js_files), ("CS", cs_files)]:
    print(f"\n=== {label} ===")
    for fname, findings in sorted(group.items()):
        print(f"\n  {fname} ({len(findings)}):")
        for line_no, cat, text in findings[:15]:
            print(f"    L{line_no} [{cat}]: {text}")
        if len(findings) > 15:
            print(f"    ... +{len(findings)-15} daha")
