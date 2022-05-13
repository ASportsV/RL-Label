import json
import random

NBA = './NBA_tasks@bk.json'
STU = './STU_tasks@bk.json'
TRA = './TRA_tasks@bk.json'

with open(TRA) as nbaf:
    data = json.load(nbaf)['tasks']
    for item in data:
        if item["type"] == 'summary':
            new_settings = []
            A = item["A"]
            for d in item["setting"]:
                point = d["point"]
                d["point"] = [
                    point[0], 
                    point[3] if d["color"] != A else random.randint(30, 50), 
                    point[2], 
                    point[1]
                ]
    print(json.dumps(data))