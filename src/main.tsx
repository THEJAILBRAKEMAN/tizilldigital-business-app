import React from 'react';import ReactDOM from 'react-dom/client';import {registerSW} from 'virtual:pwa-register';import './index.css';import App from './App';import {useLayout} from './store/useLayout';import {useSyncQueue} from './store/useSyncQueue';
useLayout.getState().init();registerSW({immediate:true});setInterval(()=>useSyncQueue.getState().processDue(),15000);
ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><App/></React.StrictMode>);
