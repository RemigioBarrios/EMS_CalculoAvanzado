import json

with open('kuta_all_options.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

print(f"Total option classes: {len(data)}")

# Let's inspect each option class and its prompts/radios/checkboxes
for k, v in sorted(data.items()):
    radios = [x for x in v if 'IDC_RADIO' in x]
    checks = [x for x in v if 'IDC_CHECK' in x]
    prompts = [x for x in v if not x.startswith('IDC_') and not x.startswith('.?AV') and not x.endswith('Options') and not x.endswith('Generator') and len(x) > 10]
    print(f"\n==========================================")
    print(f"CLASS: {k}")
    print(f"  Prompts / Titles:")
    for p in prompts[:6]:
        print(f"    - {p}")
    print(f"  Radios ({len(radios)}):")
    for r in radios[:8]:
        print(f"    - {r}")
    print(f"  Checks ({len(checks)}):")
    for c in checks[:8]:
        print(f"    - {c}")
