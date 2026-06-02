import type {ExpenseItem,LineItem,Payment} from '../types';
export const formatMUR=(value:number)=>{const fixed=(Number(value)||0).toFixed(2);const [i,d]=fixed.split('.');const sign=i.startsWith('-')?'-':'';const raw=sign?i.slice(1):i;return `Rs ${sign}${raw.replace(/\B(?=(\d{3})+(?!\d))/g,',')}.${d}`};
export const toISODate=(date=new Date())=>date.toISOString().slice(0,10);
export const formatDate=(iso:string)=>new Date(`${iso}T00:00:00`).toLocaleDateString('en-GB',{day:'2-digit',month:'short',year:'numeric'});
export const computeGrandTotal=(items:LineItem[])=>items.reduce((s,i)=>s+(Number(i.qty)||0)*(Number(i.unitPrice)||0),0);
export const computeTotalPaid=(payments:Payment[]=[],deposited=0)=>Number(deposited||0)+payments.reduce((s,p)=>s+Number(p.amount||0),0);
export const computeOutstanding=(items:LineItem[],payments:Payment[]=[],deposited=0)=>Math.max(0,computeGrandTotal(items)-computeTotalPaid(payments,deposited));
export const computeExpenseTotal=(items:ExpenseItem[])=>items.reduce((s,i)=>s+Number(i.amount||0),0);
export const downloadBlob=(name:string,content:string,type='application/json')=>{const blob=new Blob([content],{type});const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download=name;a.click();URL.revokeObjectURL(url)};
