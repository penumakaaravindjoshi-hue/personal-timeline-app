import React from 'react';
import type { TimelineEntry as TimelineEntryType } from '../../types/TimelineEntry';
import { Card, CardHeader, CardBody, Button, Chip, Image, Link } from '@heroui/react';
import { Icon } from '@iconify/react';

interface TimelineEntryProps {
    entry: TimelineEntryType;
    onEdit: (entry: TimelineEntryType) => void;
    onDelete: (id: number) => void;
}

export const TimelineEntry: React.FC<TimelineEntryProps> = ({ entry, onEdit, onDelete }) => {
    const formattedDate = new Date(entry.eventDate).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
    });

    return (
        <Card className="py-4 mb-4 relative bg-white shadow-sm border-l-4rounded-lg ">
            <CardHeader className="pb-0 pt-2 px-4 flex-col items-start">
                <p className="text-tiny uppercase font-light text-gray-500">{entry.entryType}</p>
                <small>{formattedDate}</small>
                <h4 className="font-semibold text-large text-gray-800">{entry.title}</h4>
                <div className="absolute top-2 right-2 flex space-x-1">
                    <Button
                        isIconOnly
                        onPress={() => onEdit(entry)}
                        size="sm"
                        radius="full"
                        variant="light"
                        color="primary"
                    >
                        <Icon icon="solar:pen-2-bold" width={16} height={16} />
                    </Button>
                    <Button
                        isIconOnly
                        onPress={() => onDelete(entry.id)}
                        size="sm"
                        radius="full"
                        variant="light"
                        color="danger"
                    >
                        <Icon icon="solar:trash-bin-minimalistic-bold" width={16} height={16} />
                    </Button>
                </div>
            </CardHeader>
            <CardBody className="overflow-visible">
                {entry.imageUrl && (
                    <Image
                        alt={entry.title}
                        src={entry.imageUrl}
                        className="w-[270px] h-[180px] object-cover rounded-xl"
                    />
                )}
                <div className="gap-2">
                    {entry.description && <p className="text-gray-700 mt-2">{entry.description}</p>}
                    {entry.externalUrl && (
                        <Link
                            size="sm"
                            showAnchorIcon
                            color="primary"
                            href={entry.externalUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                        >
                            View Source
                        </Link>
                    )}
                    <div className="flex flex-wrap gap-2 mt-2">
                        {entry.category && (
                            <Chip color="default" variant="flat" className="font-medium">
                                #{entry.category}
                            </Chip>
                        )}
                        {entry.sourceApi && (
                            <Chip color="primary" variant="flat" className="font-medium">
                                Source: {entry.sourceApi}
                            </Chip>
                        )}
                    </div>
                </div>
            </CardBody>
        </Card>
    );
};
