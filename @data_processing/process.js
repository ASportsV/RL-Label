const fs = require('fs')

const players = [
    { name: "K. Love", role: "PF", type: "starter",      starter: [true, true ],  on: true, team: "CLE" },
    { name: "L. James", role: "SF", type: "starter",     starter: [true, true], on: true, team: "CLE" },
    { name: "J. Smith", role: "G", type: "starter",      starter: [true, true], on: true, team: "CLE" },
    { name: "K. Irving", role: "PG", type: "starter",    starter: [true, true], on: true, team: "CLE" },
    { name: "T. Mozgov", role: "C", type: "starter",     starter: [true, true], on: true, team: "CLE" },
    { name: "T. Thompson", role: "C", type: "bench",     starter: [false, false,], on: false, team: "CLE" },
    { name: "M. Williams", role: "PG", type: "bench",    starter: [false, false,],  on: false, team: "CLE" },
    { name: "M. Dellavedova", role: "PG", type: "bench", starter: [false, false,], on: false, team: "CLE" },
    { name: "J. Jones", role: "SG", type: "bench",       starter: [false, false,], on: false, team: "CLE" },
    { name: "I. Shumpert", role: "G", type: "bench",     starter: [false, false,], on: false, team: "CLE" },
    { name: "R. Jefferson", role: "F", type: "bench",    starter: [false, false,], on: false, team: "CLE" },
    { name: "A. Varejao", role: "C", type: "bench",      starter: [false, false,], on: false, team: "CLE" },
    { name: "J. Cunningham", role: "G", type: "bench",   starter: [false, false,], on: false, team: "CLE" },

    { name: "D. Green", role: "PF", type: "starter",    starter: [true, true,],     on: true, team: "GSW" },
    { name: "S. Curry", role: "PG", type: "starter",    starter: [true, true,],     on: true, team: "GSW" },
    { name: "K. Thompson", role: "SG", type: "starter", starter: [true, true,],     on: true, team: "GSW" },
    { name: "A. Bogut", role: "C", type: "starter",     starter: [true, true,],     on: true, team: "GSW" },
    { name: "B. Rush", role: "F", type: "starter",      starter: [true, true,],     on: true, team: "GSW" },
    { name: "A. Iguodala", role: "F", type: "bench",    starter: [false, false, ],    on: false, team: "GSW" },
    { name: "M. Speights", role: "F", type: "bench",    starter: [false, false, ],    on: false, team: "GSW" },
    { name: "J. McAdoo", role: "SF", type: "bench",     starter: [false, false, ],    on: false, team: "GSW" },
    { name: "F. Ezeli", role: "C", type: "bench",       starter: [false, false, ],    on: false, team: "GSW" },
    { name: "L. Barbosa", role: "G", type: "bench",     starter: [false, false, ],    on: false, team: "GSW" },
    { name: "S. Livingston", role: "G", type: "bench",  starter: [false, false, ],    on: false, team: "GSW" },
    { name: "I. Clark", role: "G", type: "bench",       starter: [false, false, ],    on: false, team: "GSW" },
    { name: "J. Thompson", role: "PF", type: "bench",   starter: [false, false, ],    on: false, team: "GSW" }
]

const metrics = {
    'offensiveRebounder': 'OREB',
    'defensiveRebounder': 'DREB',
    'Rebounder': 'REB',
    'Assister': 'AST',
    'Stealer': 'STL',
    'Blocker': 'BLK',
    'TurnoverPlayer': 'TO',
    'Fouler': 'PF',
    '1PT': "_1PT",
    '1PT_miss': '_1PT_miss',
    '2PT': '_2PT',
    '2PT_miss': '_2PT_miss',
    '3PT': '_3PT',
    '3PT_miss': '_3PT_miss',
    'EnterGame': 'time'
}

    ; (async () => {

        const data = JSON.parse(fs.readFileSync('./data.json', { encoding: 'utf-8' }))
        //await fetch('./data.json').then(res => res.json())

        for (const item of data) {
            if (item.TurnoverCause === 'steal') {
                item.Stealer = item.TurnoverCauser
            }
            if (item.Rebounder) {
                item[`${item.ReboundType}Rebounder`] = item.Rebounder
            }
            if (item.Shooter && item.ShotType.startsWith('2-pt') && item.ShotOutcome === 'make') {
                item['2PT'] = item.Shooter
            }
            if (item.Shooter && item.ShotType.startsWith('2-pt') && item.ShotOutcome === 'miss') {
                item['2PT_miss'] = item.Shooter
            }
            if (item.Shooter && item.ShotType.startsWith('3-pt') && item.ShotOutcome === 'make') {
                item['3PT'] = item.Shooter
            }
            if (item.Shooter && item.ShotType.startsWith('3-pt') && item.ShotOutcome === 'miss') {
                item['3PT_miss'] = item.Shooter
            }
            if (item.FreeThrowShooter && item.FreeThrowOutcome === 'make') {
                item['1PT'] = item.FreeThrowShooter
            }
            if (item.FreeThrowShooter && item.FreeThrowOutcome === 'miss') {
                item['1PT_miss'] = item.FreeThrowShooter
            }
        }

        const boxes = [players.map(p => ({
            ...p,
            box: {
                ...Object.keys(metrics).reduce((o, a) => {
                    o[metrics[a]] = 0
                    return o
                }, {})
            }
        }))]

        const saveBox = []

        // console.log(boxes)
        for (const item of data) {
            const newBox = JSON.parse(JSON.stringify(boxes[boxes.length - 1]))

            const change = Object.keys(metrics)
                .reduce((o, m) => {
                    const res = addToBox(m, item, newBox)
                    return o || res
                }, false)

            if (change) {
                // printBox(newBox, item)
                saveBox.push(postProcessBox(newBox, item))
                boxes.push(newBox)
            }
        }

        console.log(saveBox[saveBox.length - 1])

        // fs.writeFileSync('boxPerRecord.json', JSON.stringify(saveBox), { encoding: 'utf-8' })
    })();


// Process points

function addToBox(key, item, newBox) {
    if (key === 'EnterGame' && item[key]) {
        const enterPlayerName = item[key].split('-')[0].trim()
        const enterPlayer = newBox.find(p => p.name === enterPlayerName)
        const leavePlayerName = item.LeaveGame.split('-')[0].trim()
        const leavePlayer = newBox.find(p => p.name === leavePlayerName)
        console.log(item.Quarter, item.SecLeft, 'EnterPlayer', enterPlayerName, 'leavePlayer', leavePlayerName)
        if (!enterPlayer || !leavePlayer) return false
        if(enterPlayer.on) console.log('The enterPlayer is already on')
        enterPlayer.on = true
        if(!leavePlayer.on) console.log('The leavePlayer is not on')
        leavePlayer.on = false
        return true
    } else if (item[key]) {
        const playerName = item[key].split('-')[0].trim()
        const player = newBox.find(p => p.name === playerName)
        if (!player) return false
        player.box[metrics[key]] += 1
        return true
    }
    return false
}

function postProcessBox(box, item) {
    const { Quarter, SecLeft } = item
    const fields = ['FG', '3PT', 'FT', ...Object.values(metrics).filter(f => !f.startsWith('_')), 'PTS']

    if(box.filter(p => p.on).length !== 10) {
        console.error('!!!!! something wrong', box.filter(p => p.on).length)
        return
    }

    return {
        Quarter, SecLeft,
        players: box.map(p => {
            const { box, ...player } = p
            // process score
            box['FG'] = `${p.box['_3PT'] + p.box['_2PT']} - ${p.box['_3PT'] + p.box['_2PT'] + p.box['_3PT_miss'] + p.box['_2PT_miss']}`
            box['3PT'] = `${p.box['_3PT']} - ${p.box['_3PT'] + p.box['_3PT_miss']}`
            box['FT'] = `${p.box['_1PT']} - ${p.box['_1PT'] + p.box['_1PT_miss']}`
            box['PTS'] = p.box['_1PT'] + p.box['_2PT'] * 2 + p.box['_3PT'] * 3

            return {
                ...player, ...fields.reduce((o, a) => {
                    o[a] = box[a]
                    return o
                }, {})
            }
        })
    }
}


function printBox(box, item) {

    const fields = ['  FG   ', ' 3PT ', ' FT  ', ...Object.values(metrics).filter(f => !f.startsWith('_')), 'PTS']

    const text = `
<tr><th>${'Name'.padEnd(14)}</th>${fields.map(f => `<th>${f}</th>`).join('')}</tr>
${box.map(p => {

        // process score
        p.box['  FG   '] = `${p.box['_3PT'] + p.box['_2PT']} - ${p.box['_3PT'] + p.box['_2PT'] + p.box['_3PT_miss'] + p.box['_2PT_miss']}`
        p.box[' 3PT '] = `${p.box['_3PT']} - ${p.box['_3PT'] + p.box['_3PT_miss']}`
        p.box[' FT  '] = `${p.box['_1PT']} - ${p.box['_1PT'] + p.box['_1PT_miss']}`
        p.box['PTS'] = p.box['_1PT'] + p.box['_2PT'] * 2 + p.box['_3PT'] * 3

        return `<tr><td>${p.name.padEnd(14)}</td>${fields.map(k => `<td>${p.box[k].toString().padStart(2).padEnd(k.length)}</td>`).join('')}</tr>`
    }).join('')}`

    document.body.insertAdjacentHTML('beforeend', `
        <h3>Quater:${item.Quarter}, Second Left: ${item.SecLeft}</h3>
        <table>
            ${text}
        </table>    
    `)

}