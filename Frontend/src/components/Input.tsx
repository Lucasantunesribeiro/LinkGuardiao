import React, { forwardRef } from 'react';
import { IconType } from 'react-icons';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: IconType;
  rightIcon?: IconType;
  onRightIconClick?: () => void;
  /** Rotulo acessivel do botao do icone direito (ex.: "Mostrar senha"). */
  rightIconLabel?: string;
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, icon: Icon, rightIcon: RightIcon, onRightIconClick, rightIconLabel, className = '', ...props }, ref) => {
    const errorId = error && props.id ? `${props.id}-error` : undefined;
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
              <Icon aria-hidden="true" className="h-5 w-5 text-stone-500" />
            </div>
          )}

          <input
            ref={ref}
            className={`
              input-field
              ${Icon ? 'pl-10' : ''}
              ${RightIcon ? 'pr-10' : ''}
              ${error ? 'border-[#c14c32] focus:ring-[#c14c32]' : ''}
              ${className}
            `}
            aria-invalid={error ? true : undefined}
            aria-describedby={errorId}
            {...props}
          />

          {RightIcon && (onRightIconClick ? (
            <button
              type="button"
              className="absolute inset-y-0 right-0 flex items-center rounded-r-2xl px-3 text-stone-500 hover:text-stone-800"
              onClick={onRightIconClick}
              aria-label={rightIconLabel}
            >
              <RightIcon aria-hidden="true" className="h-5 w-5" />
            </button>
          ) : (
            <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
              <RightIcon aria-hidden="true" className="h-5 w-5 text-stone-500" />
            </div>
          ))}
        </div>

        {error && (
          <p id={errorId} className="error-message" role="alert">
            {error}
          </p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

export default Input; 
