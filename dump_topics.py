import re

with open('CalculusApp/Generators/CalculusGenerators.cs', encoding='utf-8') as f:
    code = f.read()

topics = re.findall(r'AddTopic\(\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)"', code)
print(f"Total topics: {len(topics)}")
for tid, cat, title in topics:
    print(f"{tid:28} | {cat:16} | {title}")
