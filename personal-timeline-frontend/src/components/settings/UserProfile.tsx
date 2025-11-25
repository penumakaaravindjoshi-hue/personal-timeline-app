import React from 'react';
import { useAuth } from '../../hooks/useAuth';
import { Card, CardHeader, CardBody, Avatar } from '@heroui/react';

export const UserProfile: React.FC = () => {
    const { user } = useAuth();

    if (!user) {
        return (
            <div className="text-center py-8 text-gray-600">
                <p>User data not available.</p>
            </div>
        );
    }

    return (
        <div className="container mx-auto p-4 flex justify-center">
            <Card className="my-10 w-[500px] bg-white shadow-lg border border-gray-200">
                <CardHeader className="relative flex h-[100px] flex-col justify-end overflow-visible bg-linear-to-br  from-blue-100 via-blue-200 to-blue-300">
                    <Avatar
                        className="h-20 w-20 translate-y-12 absolute bottom-0 left-4"
                        src={user.profileImageUrl}
                        alt="Profile"
                        isBordered
                    />
                    {/* <Button
                        className="absolute top-3 right-3 bg-white/20 text-white dark:bg-black/20"
                        radius="full"
                        size="sm"
                        variant="light"
                        // onClick={() => alert("Edit Profile functionality not implemented yet")}
                    >
                        Edit Info
                    </Button> */}
                </CardHeader>
                <CardBody className="pt-16">
                    <div className="pb-4 px-4">
                        <p className="text-large font-medium text-gray-800">{user.displayName || 'N/A'}</p>
                        <p className="text-small max-w-[90%]">{user.email}</p>
                        <p className="text-small  py-2 font-light">
                            Connecting your digital life's moments. Member since{' '}
                            {new Date(user.createdAt).toLocaleDateString()}.
                        </p>
                        <div className="flex gap-4">
                            <p>
                                <span className="text-small  font-medium text-gray-700">
                                    {new Date(user.createdAt).toLocaleDateString()}
                                </span>
                                &nbsp;
                                <span className="text-small  text-gray-500">Member Since</span>
                            </p>
                            <p>
                                <span className="text-small font-medium text-gray-700">
                                    {new Date(user.lastLoginAt).toLocaleDateString()}
                                </span>
                                &nbsp;
                                <span className="text-small">Last Login</span>
                            </p>
                        </div>
                    </div>
                    {/* <Tabs fullWidth className="px-4">
                        <Tab
                            key="timeline"
                            title={
                                <Link to="/" className="block w-full text-inherit">
                                    Timeline
                                </Link>
                            }
                        />
                        <Tab
                            key="api-connections"
                            title={
                                <Link to="/settings/api-connections" className="block w-full text-inherit">
                                    API Connections
                                </Link>
                            }
                        />
                        <Tab key="settings" title="Account Settings" />
                    </Tabs> */}
                </CardBody>
            </Card>
        </div>
    );
};
