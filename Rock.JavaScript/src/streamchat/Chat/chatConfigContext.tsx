import { createContext, useContext } from "react";

interface ChatConfigContextType {
    sharedChannelTypeKey?: string;
    directMessageChannelTypeKey?: string;
}

export const ChatConfigContext = createContext<ChatConfigContextType | undefined>(undefined);

export const useChatConfig = (): ChatConfigContextType => {
    const context = useContext(ChatConfigContext);
    if (!context) {
        throw new Error("useChatConfig must be used within a ChatConfigProvider");
    }
    return context;
};
