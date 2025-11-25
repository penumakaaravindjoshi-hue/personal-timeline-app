import { useContext } from 'react';
import { TimelineContext } from '../context/TimelineContext';

export const useTimeline = () => {
  const context = useContext(TimelineContext);
  if (!context) {
    throw new Error('useTimeline must be used within a TimelineProvider');
  }
  return context;
};
