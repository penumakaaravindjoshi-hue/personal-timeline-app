import React from 'react';
import { Link } from 'react-router-dom';

export const Navigation: React.FC = () => {
    return (
        <nav className="hidden md:block">
            {' '}
            {/* Example: Hide on small screens */}
            <ul className="flex space-x-4">
                <li>
                    <Link to="/" className="hover:text-gray-300">
                        Home
                    </Link>
                </li>
                <li>
                    <Link to="/about" className="hover:text-gray-300">
                        About
                    </Link>
                </li>
                <li>
                    <Link to="/contact" className="hover:text-gray-300">
                        Contact
                    </Link>
                </li>
            </ul>
        </nav>
    );
};
