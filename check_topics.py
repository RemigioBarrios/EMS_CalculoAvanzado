import re

with open('CalculusApp/Generators/CalculusGenerators.cs', 'r', encoding='utf-8') as f:
    text = f.read()

lines = text.split('\n')
for i, l in enumerate(lines):
    if 'AddTopic(' in l:
        tid = l.split('"')[1] if '"' in l else ''
        for j in range(i, min(i+15, len(lines))):
            if '(count, type, parameters, rng)' in lines[j]:
                print(f'{tid:<25} HAS parameters')
                break
            elif '(count, type, rng)' in lines[j]:
                print(f'{tid:<25} NO parameters')
                break
