#!/bin/zsh

if [ -f .env ]; then
  export $(grep -v '^#' .env | xargs)
else
  echo "No .env file found! Exiting."
  exit 1
fi

APP_BUNDLE=$(ls -d bin/Release/net10.0-maccatalyst/*.app 2>/dev/null | head -n 1)
ZIP_FILE="EpicTuningTime.zip"

if [ -z "$APP_BUNDLE" ]; then
  echo "No app bundle found in bin/Release/net10.0-maccatalyst; run build_and_sign.sh first."
  exit 1
fi

if [ -f "$ZIP_FILE" ]; then
  echo "Removing old zip file..."
  rm "$ZIP_FILE"
fi

echo "Zipping $APP_BUNDLE..."
/usr/bin/ditto -c -k --keepParent "$APP_BUNDLE" "$ZIP_FILE"

echo "Submitting for notarization with notarytool..."
xcrun notarytool submit "$ZIP_FILE" --apple-id "$APPLE_ID" --password "$APP_PASSWORD" --team-id UA3C4N249P --wait

if [ $? -eq 0 ]; then
  echo "Notarization succeeded! Stapling the ticket to the app..."
  xcrun stapler staple "$APP_BUNDLE"
  echo "App stapled successfully!"
else
  echo "Notarization failed."
  exit 1
fi

echo "All done! 😏"
