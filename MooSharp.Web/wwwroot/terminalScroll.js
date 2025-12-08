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

window.terminalInterop = {
    initializeExitLinks: (dotNetRef, elementId) => {
        const element = document.getElementById(elementId);

        if (!element) {
            return;
        }

        element.addEventListener('click', (e) => {
            const exitLink = e.target.closest('.exit-link');

            if (exitLink) {
                const exitName = exitLink.dataset.exit;

                if (exitName) {
                    dotNetRef.invokeMethodAsync('HandleExitClick', exitName);
                }
            }
        });
    }
};
