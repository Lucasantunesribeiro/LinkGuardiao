import React from 'react';

interface LoadingProps {
  size?: 'sm' | 'md' | 'lg';
  center?: boolean;
}

const Loading: React.FC<LoadingProps> = ({ size = 'md', center = false }) => {
  const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-8 w-8',
    lg: 'h-12 w-12'
  };

  const containerClasses = center ? 'flex items-center justify-center min-h-[200px]' : '';

  return (
    <div className={containerClasses}>
      <div role="status" aria-live="polite">
        <div className={`loading-spinner ${sizeClasses[size]}`} aria-hidden="true" />
        <span className="sr-only">Carregando...</span>
      </div>
    </div>
  );
};

export default Loading; 
