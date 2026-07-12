// Bridges a drag-and-drop drop event onto a Blazor InputFile's underlying
// <input type="file">, since InputFile only reacts to a native "change"
// event and Blazor has no way to synthesize one from C# for a raw drop.
window.dragDropInterop = {
    register: function (dropZoneElement, inputElementId) {
        if (!dropZoneElement) {
            return;
        }

        dropZoneElement.addEventListener('dragover', function (event) {
            event.preventDefault();
            dropZoneElement.classList.add('drag-over');
        });

        dropZoneElement.addEventListener('dragleave', function () {
            dropZoneElement.classList.remove('drag-over');
        });

        dropZoneElement.addEventListener('drop', function (event) {
            event.preventDefault();
            dropZoneElement.classList.remove('drag-over');

            const input = document.getElementById(inputElementId);
            if (input && event.dataTransfer && event.dataTransfer.files.length > 0) {
                input.files = event.dataTransfer.files;
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    }
};
