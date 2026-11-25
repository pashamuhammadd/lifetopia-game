// Notifies C# when the page is hidden or loses focus, so held input can be released.
// Registered once by Lifetopia.Controls.WebFocusBridge. Sends no data out of the page.
mergeInto(LibraryManager.library, {
  LifetopiaRegisterFocusLossCallback: function (callback) {
    var notify = function () {
      {{{ makeDynCall('v', 'callback') }}}();
    };
    document.addEventListener('visibilitychange', function () {
      if (document.hidden) notify();
    });
    window.addEventListener('blur', notify);
    window.addEventListener('pagehide', notify);
  }
});
