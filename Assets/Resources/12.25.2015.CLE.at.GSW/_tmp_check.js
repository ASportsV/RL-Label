const fs = require('fs')
const csv=require('csvtojson')


const dir = '.'
console.log(fs.readdirSync(dir).filter(f => f.endsWith('.csv')))

;(async () => {

    const files = fs.readdirSync(dir).filter(f => f.startsWith('pos_') && f.endsWith('.csv'))
    const players = {}
    for(const f of files) {
        const jsonObj = await csv({ checkType: true }).fromFile(f)
    
        let lastRow = -2
        const intervals = []

        jsonObj.forEach((d, i) => {
            
            if(d.x_loc !== 0 || d.y_loc !== 0) {

                let interval
                if(i !== (lastRow+1)) {
                    interval = { start: i, end: -1, rows: []}
                    intervals.push(interval)
                } else {
                    interval = intervals[intervals.length - 1]
                }

                interval.rows.push(d)
                interval.end = i
                lastRow = i
            }
        })

        if (intervals.length !== 0) {
            // console.log(f, intervals.map(i => `[${i.start}, ${i.end}] -> ${i.rows.length}`), jsonObj.length)
            players[f] = intervals
        }
    } 

    const first = Object.entries(players)
        .filter(([p, intervals]) => intervals.some(int => int.start === 0) )
        
    const minTime = Math.min(...first.map(([p, intervals]) => intervals.find(i => i.start === 0).end))
    console.log(minTime)

    first
        .forEach(([p, intervals]) => {
            console.log(`Player_${p}`, intervals.map(int => `[${int.start}, ${int.end}]`))
        })


    

    for(const [p, intervals] of first) {
        const target = intervals.find(i => i.start === 0)

        // detect jump
        for(let i = 1, len = minTime + 1; i < len; ++i) {
            const row = target.rows[i]
            if(Math.abs(row.x_loc - target.rows[i-1].x_loc) > 2 && Math.abs(row.y_loc - target.rows[i-1].y_loc) > 2) {
                console.log('Player ', p, ' Jump at ', i)
            }
        }

        const cvslines = `x_loc,y_loc,game_clock,shot_clock,quarter\n` + target.rows.filter((_, i) => i <= minTime).map(row => `${row.x_loc},${row.y_loc},${row.game_clock},${row.shot_clock},${row.quarter}`).join('\n')
        fs.writeFileSync(`clean_${p}`, cvslines, { encoding: 'utf-8'})

    }

})();
