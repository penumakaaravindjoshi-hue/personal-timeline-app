import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import {
    Navbar,
    NavbarBrand,
    NavbarContent,
    NavbarItem,
    Dropdown,
    DropdownTrigger,
    DropdownMenu,
    DropdownItem,
    Avatar,
} from '@heroui/react';

export const AcmeLogo = () => {
    return (
        <svg fill="none" height="36" viewBox="0 0 32 32" width="36">
            <path
                clipRule="evenodd"
                d="M17.6482 10.1305L15.8785 7.02583L7.02979 22.5499H10.5278L17.6482 10.1305ZM19.8798 14.0457L18.11 17.1983L19.394 19.4511H16.8453L15.1056 22.5499H24.7272L19.8798 14.0457Z"
                fill="currentColor"
                fillRule="evenodd"
            />
        </svg>
    );
};

export const Header: React.FC = () => {
    const { user, isAuthenticated, logout } = useAuth();

    return (
        <Navbar isBordered className="font-light" maxWidth="full">
            <NavbarBrand>
                <AcmeLogo />
                <Link to="/" className="text-2xl  transition-colors duration-200">
                    Personal Timeline
                </Link>
            </NavbarBrand>
            <NavbarContent className="hidden sm:flex gap-4" justify="end">
                {isAuthenticated ? (
                    <>
                        <NavbarItem className="hidden md:flex">
                            <Link to="/">Timeline</Link>
                        </NavbarItem>
                        <NavbarItem className="hidden md:flex">
                            <Link to="/settings/api-connections">API Connections</Link>
                        </NavbarItem>
                        <Dropdown placement="bottom-end">
                            <DropdownTrigger>
                                <Avatar
                                    isBordered
                                    as="button"
                                    className="transition-transform"
                                    size="sm"
                                    src={user?.profileImageUrl}
                                />
                            </DropdownTrigger>
                            <DropdownMenu aria-label="Profile Actions" variant="flat">
                                <DropdownItem key="profile" className="h-14 gap-2  font-light">
                                    <p className="font-semibold">Signed in as</p>
                                    <p className="font-semibold">{user?.displayName || user?.email}</p>
                                </DropdownItem>
                                <DropdownItem key="settings">
                                    <Link to="/settings/profile" className="block w-full text-inherit font-light">
                                        My Profile
                                    </Link>
                                </DropdownItem>
                                <DropdownItem key="logout" color="danger" onClick={logout} className="font-light">
                                    Log Out
                                </DropdownItem>
                            </DropdownMenu>
                        </Dropdown>
                    </>
                ) : (
                    <></>
                )}
            </NavbarContent>
        </Navbar>
    );
};
