import React from 'react';

export const Footer: React.FC = () => {
    return (
        <footer className="font-light p-4 text-center shadow-inner mt-auto">
            <div className="container mx-auto">
                <p>&copy; {new Date().getFullYear()} Personal Timeline. All rights reserved.</p>
            </div>
        </footer>
    );
};
