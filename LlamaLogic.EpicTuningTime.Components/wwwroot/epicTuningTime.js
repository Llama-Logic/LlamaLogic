window.epicTuningTime = (function () {
    const annotationsByModel = new Map();
    const decorationsByEditor = new Map();
    const registeredLanguages = new Set();
    const hookedEditors = new WeakSet();
    const trackedScrolls = new WeakSet();
    const trackedViewStates = new WeakSet();

    function getEditor(id) {
        return window.blazorMonaco.editor.getEditor(id, true);
    }

    function findAnnotation(model, position, navigableOnly) {
        const entry = annotationsByModel.get(model.uri.toString());
        if (!entry) {
            return null;
        }
        const candidates = entry.byLine.get(position.lineNumber) || [];
        let best = null;
        for (const annotation of candidates) {
            if (navigableOnly && !annotation.navigable) {
                continue;
            }
            if (position.column >= annotation.start && position.column < annotation.end) {
                if (!best || annotation.end - annotation.start < best.end - best.start) {
                    best = annotation;
                }
            }
        }
        return best ? { annotation: best, entry: entry } : null;
    }

    function ensureHoverProvider(language) {
        if (registeredLanguages.has(language)) {
            return;
        }
        registeredLanguages.add(language);
        monaco.languages.registerHoverProvider(language, {
            provideHover: function (model, position) {
                const found = findAnnotation(model, position, false);
                if (!found || !found.annotation.hasHover || !found.entry.dotnetRef) {
                    return null;
                }
                const a = found.annotation;
                return found.entry.dotnetRef.invokeMethodAsync("GetHover", a.index).then(function (hover) {
                    return hover ? {
                        range: new monaco.Range(a.line, a.start, a.line, a.end),
                        contents: [{ value: hover, isTrusted: false, supportHtml: false }]
                    } : null;
                }).catch(function () {
                    return null;
                });
            }
        });
    }

    function activate(editor, position) {
        const model = editor.getModel();
        if (!model || !position) {
            return false;
        }
        const found = findAnnotation(model, position, true);
        if (!found) {
            return false;
        }
        found.entry.dotnetRef.invokeMethodAsync("OnAnnotationActivated", found.annotation.index).catch(function () { });
        return true;
    }

    function hookEditor(editor) {
        if (hookedEditors.has(editor)) {
            return;
        }
        hookedEditors.add(editor);
        let cursorLine = 0;
        editor.onDidChangeCursorPosition(function (e) {
            const line = e.position.lineNumber;
            const changed = line !== cursorLine;
            cursorLine = line;
            if (!changed || e.source === "api") {
                return;
            }
            const model = editor.getModel();
            const entry = model ? annotationsByModel.get(model.uri.toString()) : null;
            if (entry && entry.dotnetRef) {
                entry.dotnetRef.invokeMethodAsync("OnCursorLineChanged", line).catch(function () { });
            }
        });
        editor.onMouseDown(function (e) {
            if ((e.event.ctrlKey || e.event.metaKey) && e.target && e.target.position) {
                if (activate(editor, e.target.position)) {
                    e.event.preventDefault();
                    e.event.stopPropagation();
                }
            }
        });
        editor.addAction({
            id: "epicTuningTime.goToReference",
            label: "Go to Referenced Tuning",
            keybindings: [monaco.KeyCode.F12],
            contextMenuGroupId: "navigation",
            contextMenuOrder: 1,
            run: function (ed) {
                activate(ed, ed.getPosition());
            }
        });
    }

    function applyDecorations(editor, annotations) {
        const decorations = [];
        for (const a of annotations) {
            if (a.navigable) {
                decorations.push({ range: new monaco.Range(a.line, a.start, a.line, a.end), options: { inlineClassName: "ta-link" } });
            }
            if (a.hint) {
                decorations.push({
                    range: new monaco.Range(a.line, a.end, a.line, a.end),
                    options: { showIfCollapsed: true, after: { content: a.hint, inlineClassName: a.hintClass || "ta-hint", cursorStops: monaco.editor.InjectedTextCursorStops.None } }
                });
            }
        }
        let collection = decorationsByEditor.get(editor);
        if (!collection) {
            collection = editor.createDecorationsCollection();
            decorationsByEditor.set(editor, collection);
        }
        collection.set(decorations);
    }

    function annotate(editor, annotations, dotnetRef) {
        const model = editor.getModel();
        if (!model) {
            return;
        }
        hookEditor(editor);
        const byLine = new Map();
        (annotations || []).forEach(function (a, index) {
            a.index = index;
            if (!byLine.has(a.line)) {
                byLine.set(a.line, []);
            }
            byLine.get(a.line).push(a);
        });
        annotationsByModel.set(model.uri.toString(), { annotations: annotations || [], byLine: byLine, dotnetRef: dotnetRef });
        applyDecorations(editor, annotations || []);
    }

    function setModelContent(editor, text, language, uri) {
        let model = editor.getModel();
        const desiredUri = uri ? monaco.Uri.parse(uri) : null;
        if (!model || (desiredUri && model.uri.toString() !== desiredUri.toString())) {
            const old = model;
            model = desiredUri ? monaco.editor.getModel(desiredUri) : null;
            if (model) {
                model.setValue(text);
                monaco.editor.setModelLanguage(model, language);
            } else {
                model = monaco.editor.createModel(text, language, desiredUri || undefined);
            }
            editor.setModel(model);
            if (old && old !== model && old.uri.scheme === "tuning") {
                annotationsByModel.delete(old.uri.toString());
                old.dispose();
            }
        } else {
            if (model.getValue() !== text) {
                model.setValue(text);
            }
            monaco.editor.setModelLanguage(model, language);
        }
        return model;
    }

    let themesDefined = false;

    function defineThemes() {
        if (themesDefined || !window.monaco) {
            return;
        }
        themesDefined = true;
        monaco.editor.defineTheme("ett-dark", {
            base: "vs-dark",
            inherit: true,
            rules: [
                { token: "tag", foreground: "5ad8ff" },
                { token: "metatag", foreground: "5ad8ff" },
                { token: "delimiter", foreground: "7f8ea6" },
                { token: "attribute.name", foreground: "a78bfa" },
                { token: "attribute.value", foreground: "f9c97a" },
                { token: "comment", foreground: "5fd69a", fontStyle: "italic" },
                { token: "keyword", foreground: "a78bfa" },
                { token: "string", foreground: "f9c97a" },
                { token: "number", foreground: "7ee0c3" }
            ],
            colors: {
                "editor.background": "#00000000",
                "editorGutter.background": "#00000000",
                "minimap.background": "#00000000",
                "editorStickyScroll.background": "#0b111d",
                "editorStickyScrollGutter.background": "#0b111d",
                "editorStickyScrollHover.background": "#141c2b",
                "editorStickyScroll.shadow": "#00000080",
                "editor.lineHighlightBackground": "#ffffff0a",
                "editor.lineHighlightBorder": "#00000000",
                "editor.selectionBackground": "#5ad8ff33",
                "editor.inactiveSelectionBackground": "#5ad8ff1f",
                "editorCursor.foreground": "#5ad8ff",
                "editorLineNumber.foreground": "#4f5d73",
                "editorLineNumber.activeForeground": "#93a3bb",
                "editorIndentGuide.background": "#ffffff0d",
                "editorWidget.background": "#0c1220",
                "editorWidget.border": "#ffffff17",
                "editorHoverWidget.background": "#0c1220f2",
                "editorHoverWidget.border": "#ffffff17",
                "scrollbarSlider.background": "#ffffff14",
                "scrollbarSlider.hoverBackground": "#ffffff24",
                "scrollbarSlider.activeBackground": "#5ad8ff40",
                "diffEditor.insertedTextBackground": "#4ade8026",
                "diffEditor.removedTextBackground": "#f8717126",
                "diffEditor.insertedLineBackground": "#4ade8012",
                "diffEditor.removedLineBackground": "#f8717112"
            }
        });
        monaco.editor.defineTheme("ett-light", {
            base: "vs",
            inherit: true,
            rules: [
                { token: "tag", foreground: "0369a1" },
                { token: "metatag", foreground: "0369a1" },
                { token: "delimiter", foreground: "6b7890" },
                { token: "attribute.name", foreground: "6d28d9" },
                { token: "attribute.value", foreground: "a2440c" },
                { token: "comment", foreground: "15803d", fontStyle: "italic" },
                { token: "keyword", foreground: "6d28d9" },
                { token: "string", foreground: "a2440c" },
                { token: "number", foreground: "0f766e" }
            ],
            colors: {
                "editor.background": "#00000000",
                "editorGutter.background": "#00000000",
                "minimap.background": "#00000000",
                "editorStickyScroll.background": "#f3f6fb",
                "editorStickyScrollGutter.background": "#f3f6fb",
                "editorStickyScrollHover.background": "#e6ecf5",
                "editorStickyScroll.shadow": "#0f1e3c26",
                "editor.lineHighlightBackground": "#0f1e3c0a",
                "editor.lineHighlightBorder": "#00000000",
                "editor.selectionBackground": "#0377ad2e",
                "editor.inactiveSelectionBackground": "#0377ad1a",
                "editorCursor.foreground": "#0377ad",
                "editorLineNumber.foreground": "#97a3b6",
                "editorLineNumber.activeForeground": "#475670",
                "editorIndentGuide.background": "#0f1e3c12",
                "editorWidget.background": "#f6f9fd",
                "editorWidget.border": "#0f1e3c1f",
                "editorHoverWidget.background": "#f6f9fdf2",
                "editorHoverWidget.border": "#0f1e3c1f",
                "scrollbarSlider.background": "#0f1e3c1a",
                "scrollbarSlider.hoverBackground": "#0f1e3c2e",
                "scrollbarSlider.activeBackground": "#0377ad40",
                "diffEditor.insertedTextBackground": "#15803d26",
                "diffEditor.removedTextBackground": "#c0392b26",
                "diffEditor.insertedLineBackground": "#15803d12",
                "diffEditor.removedLineBackground": "#c0392b12"
            }
        });
    }

    if (window.require) {
        window.require(["vs/editor/editor.main"], defineThemes);
    }

    const soundPlayers = new WeakMap();
    let audioContext = null;
    let playingSound = null;

    function getAudioContext() {
        if (!audioContext) {
            audioContext = new AudioContext();
        }
        return audioContext;
    }

    function formatSoundTime(seconds) {
        const tenths = Math.floor(seconds * 10);
        const minutes = Math.floor(tenths / 600);
        const rest = (tenths % 600) / 10;
        return minutes + ":" + (rest < 10 ? "0" : "") + rest.toFixed(1);
    }

    function getSoundDuration(player) {
        return player.segments.reduce(function (total, segment) { return total + segment.duration; }, 0);
    }

    function getSoundPosition(player) {
        if (!player.playing) {
            return player.offset;
        }
        const duration = getSoundDuration(player);
        const elapsed = getAudioContext().currentTime - player.startedAt + player.startOffset;
        if (player.loop && elapsed >= duration) {
            const loopStart = player.segments.length > 1 ? player.segments[0].duration : 0;
            return duration > loopStart ? loopStart + (elapsed - loopStart) % (duration - loopStart) : 0;
        }
        return Math.max(0, Math.min(elapsed, duration));
    }

    function renderSound(player) {
        const duration = getSoundDuration(player);
        const position = getSoundPosition(player);
        const fill = player.element.querySelector(".ta-sound-fill");
        const time = player.element.querySelector(".ta-sound-time");
        if (fill) {
            fill.style.width = (duration > 0 ? position / duration * 100 : 0) + "%";
        }
        if (time) {
            time.textContent = formatSoundTime(position) + " / " + formatSoundTime(duration);
        }
    }

    function animateSound(player) {
        renderSound(player);
        if (player.playing) {
            player.frame = requestAnimationFrame(function () { animateSound(player); });
        }
    }

    function stopSoundSources(player) {
        for (const source of player.sources) {
            source.onended = null;
            try {
                source.stop();
            } catch (e) {
            }
            source.disconnect();
        }
        player.sources = [];
        cancelAnimationFrame(player.frame);
    }

    function startSoundSources(player, offset) {
        const context = getAudioContext();
        const when = context.currentTime + 0.01;
        const intro = player.segments.length > 1 ? player.segments[0] : null;
        const body = player.segments[player.segments.length - 1];
        function start(segment, at, from, loops, last) {
            segment.buffers.forEach(function (buffer, layer) {
                const source = context.createBufferSource();
                source.buffer = buffer;
                source.loop = loops;
                source.connect(player.gains[layer]);
                source.start(at, from);
                if (last && !loops && layer === 0) {
                    source.onended = function () {
                        player.playing = false;
                        player.offset = 0;
                        stopSoundSources(player);
                        renderSound(player);
                        if (playingSound === player) {
                            playingSound = null;
                        }
                        player.dotnetRef.invokeMethodAsync("OnSoundEnded").catch(function () { });
                    };
                }
                player.sources.push(source);
            });
        }
        if (intro && offset < intro.duration) {
            start(intro, when, offset, false, false);
            start(body, when + intro.duration - offset, 0, player.loop, true);
        } else {
            start(body, when, intro ? offset - intro.duration : offset, player.loop, true);
        }
        player.startedAt = when;
        player.startOffset = offset;
        player.playing = true;
        animateSound(player);
    }

    return {
        setContent: function (editorId, text, language, uri, annotations, dotnetRef, keepScroll) {
            const editor = getEditor(editorId);
            if (!editor) {
                return false;
            }
            setModelContent(editor, text, language, uri);
            ensureHoverProvider(language);
            annotate(editor, annotations, dotnetRef);
            if (!keepScroll) {
                editor.setScrollTop(0);
            }
            return true;
        },
        reveal: function (editorId, line) {
            const editor = getEditor(editorId);
            if (!editor || !line) {
                return;
            }
            editor.revealLineInCenter(line);
            editor.setSelection(new monaco.Range(line, 1, line, editor.getModel().getLineMaxColumn(line)));
            editor.focus();
        },
        setDiff: function (editorId, originalText, modifiedText, language) {
            const editor = getEditor(editorId);
            if (!editor) {
                return false;
            }
            const previous = editor.getModel();
            const original = monaco.editor.createModel(originalText, language);
            const modified = monaco.editor.createModel(modifiedText, language);
            editor.setModel({ original: original, modified: modified });
            if (previous) {
                if (previous.original) {
                    annotationsByModel.delete(previous.original.uri.toString());
                    previous.original.dispose();
                }
                if (previous.modified) {
                    annotationsByModel.delete(previous.modified.uri.toString());
                    previous.modified.dispose();
                }
            }
            return true;
        },
        setDiffModified: function (editorId, modifiedText, language) {
            const editor = getEditor(editorId);
            const previous = editor ? editor.getModel() : null;
            if (!previous || !previous.original) {
                return false;
            }
            const modified = monaco.editor.createModel(modifiedText, language);
            editor.setModel({ original: previous.original, modified: modified });
            if (previous.modified) {
                annotationsByModel.delete(previous.modified.uri.toString());
                previous.modified.dispose();
            }
            return true;
        },
        editDiffModified: function (editorId, start, end, text, expectedLength) {
            const editor = getEditor(editorId);
            const models = editor ? editor.getModel() : null;
            if (!models || !models.modified || end > models.modified.getValueLength()) {
                return false;
            }
            const model = models.modified;
            const from = model.getPositionAt(start);
            const to = model.getPositionAt(end);
            model.applyEdits([{ range: new monaco.Range(from.lineNumber, from.column, to.lineNumber, to.column), text: text }]);
            return model.getValueLength() === expectedLength;
        },
        editDiffAnnotations: function (editorId, start, removed, inserted, lineShift) {
            const editor = getEditor(editorId);
            if (!editor || !editor.getModel()) {
                return false;
            }
            const modifiedEditor = editor.getModifiedEditor();
            const model = modifiedEditor.getModel();
            const entry = model ? annotationsByModel.get(model.uri.toString()) : null;
            if (!entry || start + removed > entry.annotations.length) {
                return false;
            }
            const following = entry.annotations.slice(start + removed);
            for (const a of following) {
                a.line += lineShift;
            }
            annotate(modifiedEditor, entry.annotations.slice(0, start).concat(inserted, following), entry.dotnetRef);
            return true;
        },
        setDiffAnnotations: function (editorId, language, originalAnnotations, modifiedAnnotations, originalRef, modifiedRef) {
            const editor = getEditor(editorId);
            if (!editor || !editor.getModel()) {
                return false;
            }
            ensureHoverProvider(language);
            if (originalAnnotations) {
                annotate(editor.getOriginalEditor(), originalAnnotations, originalRef);
            }
            if (modifiedAnnotations) {
                annotate(editor.getModifiedEditor(), modifiedAnnotations, modifiedRef);
            }
            return true;
        },
        revealDiff: function (editorId, originalLine, modifiedLine) {
            const editor = getEditor(editorId);
            if (!editor) {
                return;
            }
            if (modifiedLine) {
                const modifiedEditor = editor.getModifiedEditor();
                modifiedEditor.revealLineInCenter(modifiedLine);
                modifiedEditor.setSelection(new monaco.Range(modifiedLine, 1, modifiedLine, modifiedEditor.getModel().getLineMaxColumn(modifiedLine)));
            } else if (originalLine) {
                const originalEditor = editor.getOriginalEditor();
                originalEditor.revealLineInCenter(originalLine);
                originalEditor.setSelection(new monaco.Range(originalLine, 1, originalLine, originalEditor.getModel().getLineMaxColumn(originalLine)));
            }
        },
        findLine: function (editorId, pattern) {
            const editor = getEditor(editorId);
            if (!editor || !editor.getModel()) {
                return 0;
            }
            const matches = editor.getModel().findMatches(pattern, false, true, true, null, false);
            return matches.length > 0 ? matches[0].range.startLineNumber : 0;
        },
        scrollIntoView: function (id) {
            const element = document.getElementById(id);
            if (element) {
                element.scrollIntoView({ block: "nearest" });
            }
        },
        revealVirtualized: function (container, selector, id, top, height) {
            const element = document.getElementById(id);
            if (element) {
                element.scrollIntoView({ block: "nearest" });
                return;
            }
            const scroller = container ? container.querySelector(selector) : null;
            if (!scroller) {
                return;
            }
            if (top < scroller.scrollTop) {
                scroller.scrollTop = top;
            } else if (top + height > scroller.scrollTop + scroller.clientHeight) {
                scroller.scrollTop = top + height - scroller.clientHeight;
            }
            let attempts = 0;
            const settle = function () {
                const rendered = document.getElementById(id);
                if (rendered) {
                    rendered.scrollIntoView({ block: "nearest" });
                } else if (++attempts < 120) {
                    requestAnimationFrame(settle);
                }
            };
            requestAnimationFrame(settle);
        },
        trackViewState: function (editorId, dotnetRef) {
            const editor = getEditor(editorId);
            if (!editor || trackedViewStates.has(editor)) {
                return;
            }
            trackedViewStates.add(editor);
            let timer = null;
            const report = function () {
                clearTimeout(timer);
                timer = setTimeout(function () {
                    const viewState = editor.saveViewState();
                    if (viewState) {
                        dotnetRef.invokeMethodAsync("OnViewStateChanged", JSON.stringify(viewState)).catch(function () { });
                    }
                }, 300);
            };
            const editors = editor.getOriginalEditor ? [editor.getOriginalEditor(), editor.getModifiedEditor()] : [editor];
            for (const inner of editors) {
                inner.onDidScrollChange(report);
                inner.onDidChangeCursorSelection(report);
            }
        },
        restoreViewState: function (editorId, viewState) {
            const editor = getEditor(editorId);
            if (editor && viewState) {
                editor.restoreViewState(JSON.parse(viewState));
            }
        },
        trackScroll: function (element, dotnetRef) {
            if (!element || trackedScrolls.has(element)) {
                return;
            }
            trackedScrolls.add(element);
            let timer = null;
            element.addEventListener("scroll", function () {
                clearTimeout(timer);
                timer = setTimeout(function () {
                    dotnetRef.invokeMethodAsync("OnScrolled", element.scrollTop).catch(function () { });
                }, 150);
            }, { passive: true });
        },
        setScrollTop: function (element, top) {
            if (element) {
                element.scrollTop = top;
            }
        },
        setTheme: function (dark) {
            if (window.monaco) {
                defineThemes();
                monaco.editor.setTheme(dark ? "ett-dark" : "ett-light");
            }
        },
        soundAddPart: async function (element, segment, layer, streamRef) {
            const player = soundPlayers.get(element);
            const data = await streamRef.arrayBuffer();
            const buffer = await getAudioContext().decodeAudioData(data);
            player.segments[segment].buffers[layer] = buffer;
        },
        soundCreate: function (element, dotnetRef, segmentDurations, layerCount, loop) {
            const context = getAudioContext();
            const player = {
                element: element,
                dotnetRef: dotnetRef,
                segments: segmentDurations.map(function (duration) { return { duration: duration, buffers: [] }; }),
                gains: [],
                sources: [],
                loop: loop,
                playing: false,
                offset: 0,
                startedAt: 0,
                startOffset: 0,
                frame: 0
            };
            for (let layer = 0; layer < layerCount; layer++) {
                const gain = context.createGain();
                gain.connect(context.destination);
                player.gains.push(gain);
            }
            const track = element.querySelector(".ta-sound-track");
            if (track) {
                track.addEventListener("click", function (e) {
                    const rect = track.getBoundingClientRect();
                    const fraction = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
                    const position = fraction * getSoundDuration(player);
                    if (player.playing) {
                        stopSoundSources(player);
                        startSoundSources(player, position);
                    } else {
                        player.offset = position;
                        renderSound(player);
                    }
                });
            }
            soundPlayers.set(element, player);
            renderSound(player);
        },
        soundDispose: function (element) {
            const player = soundPlayers.get(element);
            if (!player) {
                return;
            }
            stopSoundSources(player);
            for (const gain of player.gains) {
                gain.disconnect();
            }
            if (playingSound === player) {
                playingSound = null;
            }
            soundPlayers.delete(element);
        },
        soundPlay: async function (element) {
            const player = soundPlayers.get(element);
            if (!player || player.playing) {
                return;
            }
            await getAudioContext().resume();
            if (playingSound && playingSound !== player) {
                const other = playingSound;
                other.offset = getSoundPosition(other);
                other.playing = false;
                stopSoundSources(other);
                renderSound(other);
                other.dotnetRef.invokeMethodAsync("OnSoundEnded").catch(function () { });
            }
            playingSound = player;
            startSoundSources(player, player.offset >= getSoundDuration(player) ? 0 : player.offset);
        },
        soundSetLayer: function (element, layer, audible) {
            const player = soundPlayers.get(element);
            if (player && player.gains[layer]) {
                player.gains[layer].gain.value = audible ? 1 : 0;
            }
        },
        soundSetLoop: function (element, loop) {
            const player = soundPlayers.get(element);
            if (!player || player.loop === loop) {
                return;
            }
            player.loop = loop;
            if (player.playing) {
                const position = getSoundPosition(player);
                stopSoundSources(player);
                startSoundSources(player, position);
            }
        },
        soundStop: function (element) {
            const player = soundPlayers.get(element);
            if (!player || !player.playing) {
                return;
            }
            player.offset = getSoundPosition(player);
            player.playing = false;
            stopSoundSources(player);
            renderSound(player);
            if (playingSound === player) {
                playingSound = null;
            }
        }
    };
})();