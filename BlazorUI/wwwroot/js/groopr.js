window.grooprClipboard = {
    setText: function (text) {
        return navigator.clipboard.writeText(text);
    }
};

window.grooprDownload = {
    downloadText: function (filename, content, mimeType) {
        const blob = new Blob([content], { type: mimeType || 'text/plain' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = filename;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(url);
    }
};
