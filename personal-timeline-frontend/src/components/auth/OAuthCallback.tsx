import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { authService } from '../../services/authService';

export const OAuthCallback: React.FC = () => {
    const navigate = useNavigate();
    const { setAuthData } = useAuth();
    const [message, setMessage] = useState('Processing authentication...');
    console.log('OAuthCallback rendered');
    useEffect(() => {
        const handleOAuthCallback = async () => {
            try {
                // Call the backend to get user info and JWT token
                const authResponse = await authService.getAuthMe();
                console.log('OAuth callback response:', authResponse);
                setAuthData(authResponse.user, authResponse.token);
                setMessage('Authentication successful! Redirecting...');
                navigate('/'); // Redirect to home page or dashboard
            } catch (error) {
                console.error('OAuth callback failed:', error);
                setMessage('Authentication failed. Please try again.');
                navigate('/login'); // Redirect to login page on error
            }
        };

        handleOAuthCallback();
    }, [navigate, setAuthData]);

    return (
        <div className="flex items-center justify-center min-h-[calc(100vh-8rem)] bg-gray-50">
            <div className="p-8 bg-white rounded-xl shadow-lg text-center">
                <p className="text-xl font-semibold text-gray-700">{message}</p>
            </div>
        </div>
    );
};
