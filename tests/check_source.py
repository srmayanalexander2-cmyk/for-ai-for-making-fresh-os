"""Portable source checks; these are not a substitute for a WPF build or UI test."""
from pathlib import Path
from collections import Counter
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
x = '{http://schemas.microsoft.com/winfx/2006/xaml}'
for path in [*root.glob('**/*.csproj'), root/'FreshOS/App.xaml']:
    if 'obj' in path.parts or 'bin' in path.parts:
        continue
    tree = ET.parse(path)
    if path.suffix == '.xaml':
        resources = list(tree.getroot())[0]
        keys = [e.get(x+'Key') or e.get('TargetType') for e in resources]
        assert not [k for k, n in Counter(keys).items() if k and n > 1], 'Duplicate implicit style keys'
    print('XML OK:', path.relative_to(root))
assert (root/'FreshOS/Assets/Sky.jpg').read_bytes()[:3] == b'\xff\xd8\xff'
try:
    from tree_sitter import Language, Parser
    import tree_sitter_c_sharp
except ImportError:
    raise SystemExit('Install syntax dependencies: python -m pip install -r tests/requirements.txt')
parser=Parser(Language(tree_sitter_c_sharp.language()))
for path in [*root.glob('FreshOS/*.cs'), *root.glob('FreshOS.Setup/*.cs')]:
    tree=parser.parse(path.read_bytes())
    assert not tree.root_node.has_error, f'C# syntax error: {path}'
    print('C# syntax OK:', path.relative_to(root))
print('Source checks passed. WPF type checking and rendering still require Windows/.NET.')
