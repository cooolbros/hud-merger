import json
path = "Panels.json"
panels = json.loads(open(path, "r").read())
panels.sort(key=lambda i: i["Name"])
open(path, "w").write(json.dumps(panels, indent=4))