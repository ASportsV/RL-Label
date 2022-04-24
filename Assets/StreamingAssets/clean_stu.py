STU = "./student_full.csv"

clean_records = []
with open(STU) as stuf:
    records = stuf.readlines()
    for record in records:
        trackIdx, currentStep, playerIdx, px, py = record.split(',')
        if trackIdx != '10':
            clean_records.append(record)
        else:
            if playerIdx not in ['144', '151', '142', '141', '140', '146', '88', '107', '103', '116', '132']:
                clean_records.append(record)

with open('./clean_stu.csv', 'w') as stuf:
    stuf.write(''.join(clean_records))
