import React from 'react';
import { IconProps } from 'stream-chat-react/dist/types/types';

/**
 * ActionsIcon Component
 *
 * A compact SVG icon representing a three-dot menu (ellipsis), often used for
 * actions or overflow menus in UI components. Compatible with Stream's `IconProps`.
 *
 * @component
 * @example
 * <ActionsIcon className="custom-icon-class" />
 *
 * @param {string} [className] - Optional CSS class name to apply to the SVG element.
 *
 * @returns {JSX.Element} An SVG icon element representing a menu.
 */
export const ActionsIcon = ({ className = '' }: IconProps) => (
    <svg
        className={className}
        height="4"
        viewBox="0 0 11 4"
        width="11"
        xmlns="http://www.w3.org/2000/svg"
    >
        <path
            d="M1.5 3a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm4 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm4 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z"
            fillRule="nonzero"
        />
    </svg>
);
