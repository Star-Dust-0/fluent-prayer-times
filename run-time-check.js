EOF
rm run-time-check.js
node -e "
const fs=require('fs');eval(fs.readFileSync('app.js','utf8').split('async function load')[0].replace(/let data=null;/,'').replace(/function render[\s\S]*/,''));
" 2>&1 | head -3
cat > /tmp/t.js <<'EOF'
const src=require('fs').readFileSync(process.env.HOME+'/prayer/app.js','utf8');
const f=src.slice(src.indexOf('const NAMES'),src.indexOf('function render'));
eval(f+';global.c=compute;global.f=fmt');
const T={Fajr:'05:30',Sunrise:'06:57',Dhuhr:'12:48',Asr:'16:10',Maghrib:'18:39',Isha:'19:58'},U={...T,Fajr:'05:31'};
for(const h of ['2026-10-06T04:00','2026-10-06T13:00','2026-10-06T23:52']){const n=new Date(h);const r=c(n,T,U);console.log(h,r.next.k,r.tomorrowNext,f(r.next.t-n));}
