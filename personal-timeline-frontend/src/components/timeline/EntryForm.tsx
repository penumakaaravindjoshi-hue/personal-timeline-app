import React, { useState, useEffect } from 'react';
import { type TimelineEntry } from '../../types/TimelineEntry';
import { Input, Textarea, Select, SelectItem, DatePicker, Button } from '@heroui/react';
import { parseDate } from '@internationalized/date';

interface EntryFormProps {
    initialData?: TimelineEntry | null;
    onSubmit: (id: number | undefined, entry: Omit<TimelineEntry, 'userId' | 'createdAt'>) => Promise<void>;
    onCancel: () => void;
}

export const EntryForm: React.FC<EntryFormProps> = ({ initialData, onSubmit, onCancel }) => {
    const [title, setTitle] = useState(initialData?.title || '');
    const [description, setDescription] = useState(initialData?.description || '');
    const [eventDate, setEventDate] = useState(
        initialData?.eventDate ? parseDate(new Date(initialData.eventDate).toISOString().split('T')[0]) : null
    );
    const [entryType, setEntryType] = useState<TimelineEntry['entryType']>(initialData?.entryType || 'Activity');
    const [category, setCategory] = useState(initialData?.category || '');
    const [imageUrl, setImageUrl] = useState(initialData?.imageUrl || '');
    const [externalUrl, setExternalUrl] = useState(initialData?.externalUrl || '');

    useEffect(() => {
        if (initialData) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setTitle(initialData.title);
            setDescription(initialData.description || '');
            setEventDate(parseDate(new Date(initialData.eventDate).toISOString().split('T')[0]));
            setEntryType(initialData.entryType);
            setCategory(initialData.category || '');
            setImageUrl(initialData.imageUrl || '');
            setExternalUrl(initialData.externalUrl || '');
        }
    }, [initialData]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        const baseEntry = {
            title,
            eventDate: eventDate ? eventDate.toString() : '',
            entryType,
        };

        const optionalFields = {
            ...(description && { description }),
            ...(category && { category }),
            ...(imageUrl && { imageUrl }),
            ...(externalUrl && { externalUrl }),
            ...(initialData?.sourceApi && { sourceApi: initialData.sourceApi }),
            ...(initialData?.externalId && { externalId: initialData.externalId }),
            ...(initialData?.metadata && { metadata: initialData.metadata }),
            ...(initialData && { updatedAt: new Date().toISOString() }), // Only for updates
        };

        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        const entryToSubmit: Omit<TimelineEntry, 'userId' | 'createdAt'> & { id?: number } = {
            ...(initialData && { id: initialData.id }), // Include id only if initialData exists (editing)
            ...baseEntry,
            ...optionalFields,
        };
        console.log('Submitting entry:', entryToSubmit);
        await onSubmit(initialData?.id, entryToSubmit);
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-3">
            <Input
                id="title"
                label="Title"
                labelPlacement="outside"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                isRequired
                fullWidth
                variant="bordered"
                placeholder="Enter a title for your entry"
                className="text-gray-800"
            />
            <Textarea
                id="description"
                label="Description"
                labelPlacement="outside"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                fullWidth
                variant="bordered"
                placeholder="Provide a detailed description of your entry"
                className="text-gray-800"
            />
            <div className="flex flex-col md:flex-row gap-4">
                <DatePicker
                    id="eventDate"
                    label="Event Date"
                    labelPlacement="outside"
                    value={eventDate}
                    onChange={setEventDate}
                    isRequired
                    className="flex-1 text-gray-800"
                    variant="bordered"
                />
                <Select
                    id="entryType"
                    label="Entry Type"
                    labelPlacement="outside"
                    selectedKeys={[entryType]}
                    onSelectionChange={(keys) => setEntryType(Array.from(keys)[0] as TimelineEntry['entryType'])}
                    isRequired
                    className="flex-1 text-gray-800"
                    variant="bordered"
                    placeholder="Select an entry type"
                >
                    <SelectItem key="Achievement">Achievement</SelectItem>
                    <SelectItem key="Activity">Activity</SelectItem>
                    <SelectItem key="Milestone">Milestone</SelectItem>
                    <SelectItem key="Memory">Memory</SelectItem>
                </Select>
            </div>
            <Input
                id="category"
                label="Category"
                labelPlacement="outside"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                fullWidth
                variant="bordered"
                placeholder="e.g., Work, Personal, Travel"
                className="text-gray-800"
            />
            <div className="flex flex-col md:flex-row gap-4">
                <Input
                    id="imageUrl"
                    label="Image URL"
                    labelPlacement="outside"
                    type="url"
                    value={imageUrl}
                    onChange={(e) => setImageUrl(e.target.value)}
                    fullWidth
                    className="flex-1 text-gray-800"
                    variant="bordered"
                    placeholder="https://example.com/image.jpg"
                />
                <Input
                    id="externalUrl"
                    label="External URL"
                    labelPlacement="outside"
                    type="url"
                    value={externalUrl}
                    onChange={(e) => setExternalUrl(e.target.value)}
                    fullWidth
                    className="flex-1 text-gray-800"
                    variant="bordered"
                    placeholder="https://example.com/source"
                />
            </div>
            <div className="flex justify-end space-x-2 mt-6">
                <Button type="button" onPress={onCancel} variant="flat" size="sm" radius="lg">
                    Cancel
                </Button>
                <Button type="submit" color="primary" size="sm" radius="lg">
                    {initialData ? 'Update Entry' : 'Add Entry'}
                </Button>
            </div>
        </form>
    );
};
