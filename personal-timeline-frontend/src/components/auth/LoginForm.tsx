import React from 'react';
import { Button } from '@heroui/react';
import { Icon } from '@iconify/react';
import { useAuth } from '../../hooks/useAuth'; // Import useAuth

export const LoginForm: React.FC = () => {
    // Changed to named export
    const { login } = useAuth(); // Use the useAuth hook

    return (
        <div className="flex justify-center items-center h-screen w-full">
            {/* Login Form */}
            <div className="rounded-large bg-content1 shadow-small flex w-full max-w-sm flex-col gap-4 px-10 pt-8 pb-10">
                <p className="pb-2 text-xl font-medium text-center">Log In to Personal Timeline</p>{' '}
                {/* Added text-center and "to Personal Timeline" */}
                <div className="flex flex-col gap-2">
                    <Button
                        startContent={<Icon icon="flat-color-icons:google" width={24} />}
                        variant="bordered"
                        onPress={login} // Call the login function from useAuth
                    >
                        Continue with Google
                    </Button>
                    {/* <Button
                        startContent={<Icon className="text-default-500" icon="fe:github" width={24} />}
                        variant="bordered"
                        isDisabled={true} // Disable the button
                    >
                        Continue with Github
                    </Button> */}
                </div>
            </div>
        </div>
    );
};
