STU = "./train_tasks.csv"

clean_records = []
with open(STU) as stuf:
    records = stuf.readlines()
    for record in records:
        trackIdx, currentStep, playerIdx, px, py = record.split(',')
        if trackIdx == '5':
            if playerIdx not in ['123','132', '133', '138', '141', '143', '144', '145', '120', '130', '121', '124', '136', '157', '154', '160', '121', '146', '150', '149']:
                clean_records.append(record)
        elif trackIdx == '4':
            if playerIdx not in ['20', '21', '22', '23', '24', '30', '29', '4', '14']:
                clean_records.append(record)
        elif trackIdx == '3':
            if playerIdx not in ['7', '8']:
                clean_records.append(record)
        else :
            clean_records.append(record)

with open('./clean_tra.csv', 'w') as stuf:
    stuf.write(''.join(clean_records))
