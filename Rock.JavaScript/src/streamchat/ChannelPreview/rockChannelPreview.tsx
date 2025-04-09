import React, { useRef, useEffect, useState } from 'react';
import clsx from 'clsx';
import {
    ChannelPreviewUIComponentProps,
    DefaultStreamChatGenerics,
    DialogManagerProvider,
    useComponentContext,
    useChatContext,
} from 'stream-chat-react';
import { Avatar as DefaultAvatar } from 'stream-chat-react';
import { RockChannelPreviewActionButtons } from './RockChannelActionButtons';

const UnMemoizedChannelPreviewMessenger = <
    SCG extends DefaultStreamChatGenerics = DefaultStreamChatGenerics,
>(
    props: ChannelPreviewUIComponentProps<SCG>,
) => {
    const {
        active,
        Avatar = DefaultAvatar,
        channel,
        className: customClassName = '',
        displayImage,
        displayTitle,
        groupChannelDisplayInfo,
        latestMessagePreview,
        onSelect: customOnSelectChannel,
        setActiveChannel,
        unread,
        watchers,
    } = props;

    const { client } = useChatContext<SCG>();
    const { ChannelPreviewActionButtons = RockChannelPreviewActionButtons } = useComponentContext<SCG>();
    const channelPreviewButton = useRef<HTMLButtonElement | null>(null);
    const isMuted = channel.muteStatus().muted;

    const avatarName =
        displayTitle || channel?.state?.messages?.at(-1)?.user?.id;

    const onSelectChannel = (e: React.MouseEvent<HTMLButtonElement>) => {
        if (customOnSelectChannel) {
            customOnSelectChannel(e);
        } else if (setActiveChannel && channel) {
            setActiveChannel(channel, watchers);
        }
        channelPreviewButton.current?.blur();
    };

    return (
        <DialogManagerProvider>
            <div className='str-chat__channel-preview-container'>
                {channel && <ChannelPreviewActionButtons channel={channel} />}
                <button
                    aria-label={`Select Channel: ${displayTitle || ''}`}
                    aria-selected={active}
                    className={clsx(
                        'str-chat__channel-preview-messenger str-chat__channel-preview',
                        active && 'str-chat__channel-preview-messenger--active',
                        unread && unread >= 1 && 'str-chat__channel-preview-messenger--unread',
                        isMuted && 'str-chat__channel-preview--muted',
                        customClassName,
                    )}
                    data-testid='channel-preview-button'
                    onClick={onSelectChannel}
                    ref={channelPreviewButton}
                    role='option'
                >
                    <div className='str-chat__channel-preview-messenger--left'>
                        <Avatar
                            className='str-chat__avatar--channel-preview'
                            groupChannelDisplayInfo={groupChannelDisplayInfo}
                            image={displayImage}
                            name={avatarName}
                        />
                    </div>
                    <div className='str-chat__channel-preview-end'>
                        <div className='str-chat__channel-preview-end-first-row'>
                            <div className='str-chat__channel-preview-messenger--name'>
                                <span>
                                    {displayTitle}
                                    {isMuted && <span title="Muted"> 🔇</span>}
                                </span>
                            </div>
                            {!!unread && (
                                <div
                                    className='str-chat__channel-preview-unread-badge'
                                    data-testid='unread-badge'
                                >
                                    {unread}
                                </div>
                            )}
                        </div>
                        <div className='str-chat__channel-preview-messenger--last-message'>
                            {isMuted && <span title="Muted">Channel is muted</span>}
                            {!isMuted && <span>{latestMessagePreview}</span>}
                        </div>
                    </div>
                </button>
            </div>
        </DialogManagerProvider>
    );
};

export const RockChannelPreview = React.memo(
    UnMemoizedChannelPreviewMessenger,
) as typeof UnMemoizedChannelPreviewMessenger;
