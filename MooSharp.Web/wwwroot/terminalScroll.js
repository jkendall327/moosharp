window.terminalScroll = {
    scrollToBottom: (elementId) => {
        const element = document.getElementById(elementId);

        if (!element) {
            return;
        }

        element.scrollTop = element.scrollHeight;
    },
    scrollToTop: (elementId) => {
        const element = document.getElementById(elementId);

        if (!element) {
            return;
        }

        element.scrollTop = 0;
    }
};
