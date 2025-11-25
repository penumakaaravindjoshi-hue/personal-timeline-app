import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import { Header } from './components/layout/Header';
import { Footer } from './components/layout/Footer';
import { LoginForm } from './components/auth/LoginForm';
import { OAuthCallback } from './components/auth/OAuthCallback';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { TimelineView } from './components/timeline/TimelineView';
import { ApiConnections } from './components/settings/ApiConnections';
import { UserProfile } from './components/settings/UserProfile';

function App() {
    const { isAuthenticated, loading } = useAuth();
    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen bg-gray-100">
                <p className="text-xl font-semibold text-gray-700">Loading application...</p>
            </div>
        );
    }

    return (
        <div className="flex flex-col min-h-screen">
            <Header />
            <main className="h-full">
                <Routes>
                    <Route path="/login" element={!isAuthenticated ? <LoginForm /> : <Navigate to="/" />} />
                    <Route path="/auth/callback" element={<OAuthCallback />} />

                    <Route element={<ProtectedRoute />}>
                        <Route path="/" element={<TimelineView />} />
                        <Route path="/settings/api-connections" element={<ApiConnections />} />
                        <Route path="/settings/profile" element={<UserProfile />} />
                    </Route>

                    <Route path="*" element={<Navigate to="/" />} />
                </Routes>
            </main>
            <Footer />
        </div>
    );
}

export default App;
