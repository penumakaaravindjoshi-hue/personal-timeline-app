import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

export const ProtectedRoute: React.FC = () => {
    const { isAuthenticated, loading } = useAuth();

    if (loading) {
        // Optionally render a loading spinner or placeholder
        return (
            <div className="flex items-center justify-center min-h-[calc(100vh-8rem)]">
                <p className="text-gray-600">Loading user session...</p>
            </div>
        );
    }

    return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
};
