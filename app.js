const NAMES={Fajr:'الفجر',Sunrise:'الشروق',Dhuhr:'الظهر',Asr:'العصر',Maghrib:'المغرب',Isha:'العشاء'};
const KEYS=Object.keys(NAMES);
let data=null;
function at(day,hm,add=0){const[h,m]=hm.split(':').map(Number);return new Date(day.getFullYear(),day.getMonth(),day.getDate()+add,h,m,0);}
function compute(now,today,tomorrow){
  const list=KEYS.map(k=>({k,t:at(now,today[k])}));
  let next=list.find(p=>p.t>now),tomorrowNext=false;
  if(!next&&tomorrow){next={k:'Fajr',t:at(now,tomorrow.Fajr,1)};tomorrowNext=true;}
  return{list,next,tomorrowNext};
}
function pad(n){return String(n).padStart(2,'0');}
function fmt(ms){const s=Math.max(0,Math.floor(ms/1000));return`${pad(Math.floor(s/3600))}:${pad(Math.floor(s%3600/60))}:${pad(s%60)}`;}
function ymd(d){return`${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}`;}
function render(){
  if(!data||!data.today){document.getElementById('off').textContent=data?'تعذر تحميل المواقيت - تأكد من الاتصال بالإنترنت':'';return;}
  const now=new Date();
  if(data.date!==ymd(now)){load();return;}
  const{list,next,tomorrowNext}=compute(now,data.today,data.tomorrow);
  document.getElementById('list').innerHTML=list.map(p=>{
    const c=(!tomorrowNext&&next&&p.k===next.k)?'next':(p.t<=now?'past':'');
    return`<div class="row ${c}"><span>${NAMES[p.k]}</span><span class="t">${data.today[p.k]}</span></div>`;}).join('');
  document.getElementById('nn').textContent=next?NAMES[next.k]+(tomorrowNext?' (غدًا)':''):'--';
  document.getElementById('cd').textContent=next?fmt(next.t-now):'--:--:--';
  document.getElementById('date').textContent='الإسكندرية، مصر - '+now.toLocaleDateString('ar-EG-u-nu-latn',{weekday:'long',day:'numeric',month:'long'});
  document.getElementById('off').textContent=data.offline?'غير متصل - يتم عرض آخر مواقيت محفوظة':'';
}
async function load(){data=await window.api.times();render();}
window.api.onRefresh(load);
load();setInterval(render,1000);setInterval(load,30*60*1000);
if(location.hash==='#test'){window.__compute=compute;}
