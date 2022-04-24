STU = "./train_tasks.csv"

clean_records = []
with open(STU) as stuf:
    records = stuf.readlines()
    for record in records:
        trackIdx, currentStep, playerIdx, px, py = record.split(',')
        if trackIdx != '5':
            clean_records.append(record)
        else:
            if playerIdx not in ['123','132', '133', '138', '141', '143', '144', '145', '120', '130', '121', '124', '136', '157', '154', '160', '121', '146', '150', '149']:
                clean_records.append(record)

with open('./clean_tra.csv', 'w') as stuf:
    stuf.write(''.join(clean_records))
