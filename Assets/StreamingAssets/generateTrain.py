import random


nba = './nba_full_split.csv'
stu = './student_full.csv'

nba_group_by_track = {}
nba_test_track = [0, 13, 15, 16, 21, 22]
with open(nba) as nbaf:
    records = nbaf.readlines()
    print(len(records))
    for record in records:
        trackIdx, playerIdx, px, py = record.split(',')
        currentStep = 0
        if int(trackIdx) in nba_test_track: continue

        if trackIdx not in nba_group_by_track:
            nba_group_by_track[trackIdx] = []
        nba_group_by_track[trackIdx].append([trackIdx, currentStep, playerIdx, px, py])

stu_group_by_track = {}
stu_test_track = [4, 8, 16, 25, 12, 10]
with open(stu) as stuf:
    records = stuf.readlines()
    print(len(records))
    for record in records:
        trackIdx, currentStep, playerIdx, px, py = record.split(',')
        if int(trackIdx) in stu_test_track: continue

        if trackIdx not in stu_group_by_track:
            stu_group_by_track[trackIdx] = []
        stu_group_by_track[trackIdx].append([trackIdx, currentStep, playerIdx, px, py])


print("nba tracks " + str(len(nba_group_by_track.keys())))
print("stu tracks " + str(len(stu_group_by_track.keys())))

trainning_records = []
for tIdx, group in enumerate([nba_group_by_track, stu_group_by_track]):
    track = random.choice(list(group.values()))
    trainning_records += [",".join(map(str, [tIdx] + t[1:])) for t in track]
print("Trainning records " + str(len(trainning_records)))

trainning = "train_tasks.csv"
with open(trainning, "w") as trainF:
    trainF.write("".join(trainning_records))