// Browser helpers for the WebGL build: file download (session log) and file open (replay a log).
mergeInto(LibraryManager.library, {
  WarehouseDownloadText: function (namePtr, textPtr) {
    var name = UTF8ToString(namePtr);
    var text = UTF8ToString(textPtr);
    var blob = new Blob([text], { type: "application/x-ndjson" });
    var a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = name;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { document.body.removeChild(a); URL.revokeObjectURL(a.href); }, 100);
  },
  WarehouseOpenTextFile: function (goPtr, methodPtr) {
    var go = UTF8ToString(goPtr);
    var method = UTF8ToString(methodPtr);
    var input = document.createElement("input");
    input.type = "file";
    input.accept = ".jsonl,.txt,application/x-ndjson";
    input.style.display = "none";
    input.onchange = function () {
      var file = input.files && input.files[0];
      if (!file) return;
      var reader = new FileReader();
      reader.onload = function () {
        if (window.unityInstance) window.unityInstance.SendMessage(go, method, reader.result);
        document.body.removeChild(input);
      };
      reader.readAsText(file);
    };
    document.body.appendChild(input);
    input.click();
  },
  WarehouseSetStatus: function (jsonPtr) {
    window.warehouseStatus = UTF8ToString(jsonPtr);
  },
  WarehouseNotifyReady: function () {
    if (window.onWarehouseReady) window.onWarehouseReady();
  }
});
