import React from 'react';
import './rockBadge.css';

interface RockBadgeProps {
    badgeText: string;
    foregroundColor?: string;
    backgroundColor?: string;
}

const RockBadge: React.FC<RockBadgeProps> = ({
    badgeText,
    foregroundColor,
    backgroundColor
}) => {
    const style = {
        color: foregroundColor || '#000',
        backgroundColor: backgroundColor || '#f0f0f0'
    };

    return (
        <div className="rock-badge" style={style}>
            {badgeText}
        </div>
    );
};

export default RockBadge;
