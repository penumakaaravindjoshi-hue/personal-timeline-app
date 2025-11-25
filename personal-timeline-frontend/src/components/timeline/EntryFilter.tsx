import React, { useState } from 'react';
import { Input, Select, SelectItem } from '@heroui/react';

interface EntryFilterProps {
    onFilterChange: (filters: { search: string; type: string; category: string }) => void;
}

export const EntryFilter: React.FC<EntryFilterProps> = ({ onFilterChange }) => {
    const [search, setSearch] = useState('');
    const [type, setType] = useState('');
    const [category, setCategory] = useState('');

    const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setSearch(e.target.value);
        onFilterChange({ search: e.target.value, type, category });
    };

    const handleCategoryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setCategory(e.target.value);
        onFilterChange({ search, type, category: e.target.value });
    };

    return (
        <div className="bg-white shadow-md rounded-lg p-4 mb-6">
            <h2 className="text-xl font-light mb-4">Filter Timeline</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                    <Input
                        id="search"
                        value={search}
                        onChange={handleSearchChange}
                        placeholder="Search by title or description"
                        label="Search"
                        labelPlacement="outside"
                        onClear={() => {
                            setSearch('');
                            onFilterChange({ search: '', type, category });
                        }}
                        isClearable
                    />
                </div>
                <div>
                    <Select
                        id="type"
                        selectedKeys={[type]}
                        onSelectionChange={(keys) => {
                            const selectedType = Array.from(keys)[0] as string;
                            setType(selectedType);
                            onFilterChange({ search, type: selectedType, category });
                        }}
                        label="Entry Type"
                        labelPlacement="outside"
                    >
                        <SelectItem key="">All Types</SelectItem>
                        <SelectItem key="Achievement">Achievement</SelectItem>
                        <SelectItem key="Activity">Activity</SelectItem>
                        <SelectItem key="Milestone">Milestone</SelectItem>
                        <SelectItem key="Memory">Memory</SelectItem>
                    </Select>
                </div>
                <div>
                    <Input
                        id="category"
                        value={category}
                        onChange={handleCategoryChange}
                        placeholder="Filter by category"
                        label="Category"
                        labelPlacement="outside"
                        onClear={() => {
                            setCategory('');
                            onFilterChange({ search, type, category: '' });
                        }}
                        isClearable
                    />
                </div>
            </div>
        </div>
    );
};
