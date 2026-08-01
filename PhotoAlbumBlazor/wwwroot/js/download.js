// Triggers a browser file download from an in-memory data URL. Needed because
// the app fetches photo bytes through an authenticated HttpClient call (the
// API requires a Bearer token an <a href> can't attach) rather than linking
// directly to the API, so there's no plain URL a native download could use.
window.downloadHelper = {
    saveDataUrl: function (dataUrl, fileName) {
        const anchor = document.createElement('a');
        anchor.href = dataUrl;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
    }
};
