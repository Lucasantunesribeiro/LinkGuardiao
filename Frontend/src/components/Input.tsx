import React, { forwardRef } from 'react';
import { IconType } from 'react-icons';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: IconType;
  rightIcon?: IconType;
  onRightIconClick?: () => void;
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, icon: Icon, rightIcon: RightIcon, onRightIconClick, className = '', ...props }, ref) => {
    return (
      <div className="form-group">
        {label && (
          <label htmlFor={props.id} className="form-label">
            {label}
          </label>
        )}
        
        <div className="relative">
          {Icon && (
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <Icon className="h-5 w-5 text-gray-400" />
            </div>
          )}

          <input
            ref={ref}
            className={`
              input-field
              ${Icon ? 'pl-10' : ''}
              ${RightIcon ? 'pr-10' : ''}
              ${error ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}
              ${className}
            `}
            {...props}
          />

          {RightIcon && (
            <div
              className={`
                absolute inset-y-0 right-0 pr-3 flex items-center
                ${onRightIconClick ? 'cursor-pointer hover:text-gray-700' : 'pointer-events-none'}
              `}
              onClick={onRightIconClick}
            >
              <RightIcon className="h-5 w-5 text-gray-400" />
            </div>
          )}
        </div>

        {error && (
          <p className="error-message">
            {error}
          </p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

export default Input; 