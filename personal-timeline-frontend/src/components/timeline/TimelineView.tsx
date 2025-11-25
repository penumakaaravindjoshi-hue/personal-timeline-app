import React, { useState, useMemo } from 'react';
import { useTimeline } from '../../hooks/useTimeline';
import type { TimelineEntry } from '../../types/TimelineEntry';
import { EntryForm } from './EntryForm';
import { TimelineEntry as TimelineEntryComponent } from './TimelineEntry'; // Renamed to avoid conflict
import { EntryFilter } from './EntryFilter'; // Import EntryFilter
import {
    Button,
    Modal,
    ModalContent,
    ModalHeader,
    ModalBody,
    ModalFooter,
    useDisclosure,
    addToast,
} from '@heroui/react';
import { Icon } from '@iconify/react';

export const TimelineView: React.FC = () => {
    const [searchFilter, setSearchFilter] = useState('');
    const [typeFilter, setTypeFilter] = useState('');
    const [categoryFilter, setCategoryFilter] = useState('');

    const { timelineEntries, loading, error, addEntry, updateEntry, deleteEntry } = useTimeline();
    const { isOpen: isFormOpen, onOpen: onOpenForm, onOpenChange: onOpenChangeForm } = useDisclosure();
    const { isOpen: isConfirmOpen, onOpen: onOpenConfirm, onOpenChange: onOpenChangeConfirm } = useDisclosure();
    const [editingEntry, setEditingEntry] = useState<TimelineEntry | null>(null);
    const [entryToDeleteId, setEntryToDeleteId] = useState<number | null>(null);

    const handleFilterChange = (filters: { search: string; type: string; category: string }) => {
        setSearchFilter(filters.search);
        setTypeFilter(filters.type);
        setCategoryFilter(filters.category);
    };

    const filteredEntries = useMemo(() => {
        return timelineEntries
            .filter((entry) => {
                const matchesSearch =
                    entry.title.toLowerCase().includes(searchFilter.toLowerCase()) ||
                    (entry.description && entry.description.toLowerCase().includes(searchFilter.toLowerCase()));
                const matchesType = typeFilter ? entry.entryType === typeFilter : true;
                const matchesCategory = categoryFilter
                    ? entry.category && entry.category.toLowerCase().includes(categoryFilter.toLowerCase())
                    : true;
                return matchesSearch && matchesType && matchesCategory;
            })
            .sort((a, b) => new Date(b.eventDate).getTime() - new Date(a.eventDate).getTime());
    }, [timelineEntries, searchFilter, typeFilter, categoryFilter]);

    const handleAddEntry = async (
        _id: number,
        entry: Omit<TimelineEntry, 'id' | 'userId' | 'createdAt' | 'updatedAt'>
    ) => {
        console.log('Adding timeline entry:', entry);
        await addEntry(entry);
        onOpenChangeForm(); // Close modal
        addToast({
            title: 'Entry Added',
            description: 'Your timeline entry has been successfully added.',
        });
    };

    const handleUpdateEntry = async (id: number, entry: Omit<TimelineEntry, 'userId' | 'createdAt'>) => {
        await updateEntry(id, entry);
        setEditingEntry(null);
        onOpenChangeForm(); // Close modal
        addToast({
            title: 'Entry Updated',
            description: 'Your timeline entry has been successfully updated.',
        });
    };

    const handleDeleteClick = (id: number) => {
        setEntryToDeleteId(id);
        onOpenConfirm();
    };

    const handleConfirmDelete = async () => {
        if (entryToDeleteId !== null) {
            await deleteEntry(entryToDeleteId);
            addToast({
                title: 'Entry Deleted',
                description: 'Your timeline entry has been successfully deleted.',
            });
            setEntryToDeleteId(null);
            onOpenChangeConfirm(); // Close confirmation modal
        }
    };

    const openEditForm = (entry: TimelineEntry) => {
        setEditingEntry(entry);
        onOpenForm(); // Open modal
    };

    if (loading) {
        return <div className="text-center py-8">Loading timeline entries...</div>;
    }

    if (error) {
        return <div className="text-center py-8 text-red-500">Error: {error}</div>;
    }

    return (
        <div className="container mx-auto p-4 mt-2">
            <div className="flex items-center justify-between mb-3">
                <h1 className="text-2xl font-light">Your Personal Timeline</h1>

                <Button
                    onPress={() => {
                        setEditingEntry(null);
                        onOpenForm();
                    }}
                    color="primary"
                    size="sm"
                    className="font-light"
                    radius="lg"
                    startContent={<Icon icon="mdi:plus" className="w-4 h-4" />}
                >
                    Add New Entry
                </Button>
            </div>

            <EntryFilter onFilterChange={handleFilterChange} />

            <Modal isOpen={isFormOpen} onOpenChange={onOpenChangeForm} placement="top-center" size="xl">
                <ModalContent>
                    {(onClose) => (
                        <>
                            <ModalHeader className="flex flex-col gap-1 font-light">
                                {editingEntry ? 'Edit Timeline Entry' : 'Add New Timeline Entry'}
                            </ModalHeader>
                            <ModalBody className="">
                                <EntryForm
                                    initialData={editingEntry}
                                    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                                    // @ts-ignore
                                    onSubmit={editingEntry ? handleUpdateEntry : handleAddEntry}
                                    onCancel={onClose}
                                />
                            </ModalBody>
                        </>
                    )}
                </ModalContent>
            </Modal>

            <Modal isOpen={isConfirmOpen} onOpenChange={onOpenChangeConfirm}>
                <ModalContent>
                    {(onClose) => (
                        <>
                            <ModalHeader className="flex flex-col font-normal">Confirm Deletion</ModalHeader>
                            <ModalBody>
                                <p className="font-light">
                                    Are you sure you want to delete this timeline entry? This action cannot be undone.
                                </p>
                            </ModalBody>
                            <ModalFooter>
                                <Button color="default" variant="light" onPress={onClose} size="sm" radius="lg">
                                    Cancel
                                </Button>
                                <Button color="danger" onPress={handleConfirmDelete} size="sm" radius="lg">
                                    Delete
                                </Button>
                            </ModalFooter>
                        </>
                    )}
                </ModalContent>
            </Modal>

            {filteredEntries.length === 0 ? (
                <p className="text-center text-gray-600 text-lg">No timeline entries yet. Add one to get started!</p>
            ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                    {filteredEntries.map((entry) => (
                        <TimelineEntryComponent
                            key={entry.id}
                            entry={entry}
                            onEdit={openEditForm}
                            onDelete={handleDeleteClick}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};
