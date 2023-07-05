mergeInto(LibraryManager.library, {
  GameOver: function (nickname) {
    window.dispatchReactUnityEvent("GameOver", UTF8ToString(nickname));
  },
});