import json
NBA = './TRA_tasks'

new_tasks = []
i_tasks = []
c_tasks = []
s_tasks = []
with open(NBA + ".json") as nbaf:
    tasks = json.load(nbaf)['tasks']
    print(len(tasks))
    i_tasks = [t for t in tasks if t["type"] == "identify"]
    c_tasks = [t for t in tasks if t["type"] == "compare"]
    s_tasks = [t for t in tasks if t["type"] == "summary"]
    new_tasks += i_tasks[:3]
    new_tasks += c_tasks[:3]
    new_tasks += s_tasks[:3]
    new_tasks += i_tasks[3:]
    new_tasks += c_tasks[3:]
    new_tasks += s_tasks[3:]

with open(NBA+ "_reorder.json", "w") as nbaf:
    json.dump({"tasks":new_tasks}, nbaf)