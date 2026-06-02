import {create} from 'zustand';import {useSettingsStore} from './useSettingsStore';
type Mode='mobile'|'desktop';type State={detected:Mode;init:()=>void;resolved:()=>Mode};
const detect=():Mode=>typeof window!=='undefined'&&window.matchMedia('(min-width:1024px)').matches?'desktop':'mobile';
export const useLayout=create<State>((set,get)=>({detected:detect(),init:()=>{let t:number|undefined;const fn=()=>{clearTimeout(t);t=window.setTimeout(()=>set({detected:detect()}),120)};window.addEventListener('resize',fn);set({detected:detect()})},resolved:()=>{const m=useSettingsStore.getState().settings.layoutMode;return m==='auto'?get().detected:m}}));
export const useLayoutMode=()=>useLayout(s=>{const m=useSettingsStore.getState().settings.layoutMode;return m==='auto'?s.detected:m});
