mergeInto(LibraryManager.library, {
  WebCopyToClipboard: function (textPtr) {
    var text = UTF8ToString(textPtr);

    function fallbackCopy() {
      try {
        var textarea = document.createElement("textarea");
        textarea.value = text;
        textarea.setAttribute("readonly", "");
        textarea.style.position = "fixed";
        textarea.style.left = "-9999px";
        textarea.style.top = "0";
        document.body.appendChild(textarea);

        textarea.focus();
        textarea.select();
        textarea.setSelectionRange(0, textarea.value.length);

        var successful = document.execCommand("copy");
        document.body.removeChild(textarea);

        if (successful) {
          console.log("[Clipboard] Copied using execCommand: " + text);
          return true;
        }
      } catch (err) {
        console.error("[Clipboard] execCommand fallback failed:", err);
      }

      return false;
    }

    // Try old fallback first because it often works better inside iframes.
    if (fallbackCopy()) {
      return;
    }

    if (navigator.clipboard && window.isSecureContext) {
      navigator.clipboard.writeText(text).then(function () {
        console.log("[Clipboard] Copied using navigator.clipboard: " + text);
      }).catch(function (err) {
        console.error("[Clipboard] navigator.clipboard failed:", err);

        // Last resort: show the code so the player can copy manually.
        window.prompt("Copy this room code:", text);
      });
    } else {
      console.warn("[Clipboard] Clipboard API unavailable. Showing prompt.");
      window.prompt("Copy this room code:", text);
    }
  }
});