import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import './index.css';
import App from './App.tsx';
import { AuthProvider } from './context/AuthContext.tsx';
import { TimelineProvider } from './context/TimelineContext.tsx';
import { HeroUIProvider, ToastProvider } from '@heroui/react';
createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <BrowserRouter>
            <AuthProvider>
                <TimelineProvider>
                    <HeroUIProvider>
                        <ToastProvider />
                        <App />
                    </HeroUIProvider>
                </TimelineProvider>
            </AuthProvider>
        </BrowserRouter>
    </StrictMode>
);
