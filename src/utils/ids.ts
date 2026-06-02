export const newId=()=>crypto.randomUUID?crypto.randomUUID():`${Date.now()}-${Math.random().toString(16).slice(2)}`;
const yy=()=>String(new Date().getFullYear()).slice(-2); const seq=(n:number,w:number)=>String(n+1).padStart(w,'0');
export const generateCustomerCode=(count:number)=>`CUS${seq(count,3)}`;
export const generateAgreementNo=(count:number)=>`AGR${yy()}${seq(count,4)}`;
export const generateInvoiceNo=(count:number)=>`INV${seq(count,4)}`;
export const generateDeliveryNo=(count:number)=>`KD${yy()}${seq(count,4)}`;
export const generateVoucherNo=(count:number)=>`EXP${yy()}${seq(count,4)}`;
export const newReceiptRef=(prefix='RCT')=>`${prefix}${yy()}${Date.now().toString().slice(-6)}`;
