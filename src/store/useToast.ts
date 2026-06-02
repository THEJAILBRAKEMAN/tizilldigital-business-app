import {create} from 'zustand';import {newId} from '../utils/ids';
export type ToastType='success'|'error'|'warning'|'info';export type Toast={id:string;type:ToastType;message:string};
type State={toasts:Toast[];push:(type:ToastType,message:string)=>void;success:(m:string)=>void;error:(m:string)=>void;warning:(m:string)=>void;info:(m:string)=>void;remove:(id:string)=>void};
export const useToast=create<State>((set,get)=>({toasts:[],push:(type,message)=>{const id=newId();set({toasts:[{id,type,message},...get().toasts].slice(0,5)});setTimeout(()=>get().remove(id),3500)},success:m=>get().push('success',m),error:m=>get().push('error',m),warning:m=>get().push('warning',m),info:m=>get().push('info',m),remove:id=>set({toasts:get().toasts.filter(t=>t.id!==id)})}));
