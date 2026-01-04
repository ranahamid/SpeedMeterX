// SpeedMeterX Speed Test - Client-side JavaScript for accurate speed measurements

window.SpeedTest = {
    // Cloudflare speed test endpoints
    cloudflareDownload: 'https://speed.cloudflare.com/__down?bytes=',
    cloudflareUpload: 'https://speed.cloudflare.com/__up',
    
    // Control flags
    isPaused: false,
    isStopped: false,
    
    // Pause the test
    pause: function() {
        this.isPaused = true;
    },
    
    // Resume the test
    resume: function() {
        this.isPaused = false;
    },
    
    // Stop the test completely
    stop: function() {
        this.isStopped = true;
        this.isPaused = false;
    },
    
    // Reset control flags
    reset: function() {
        this.isPaused = false;
        this.isStopped = false;
    },
    
    // Check if should continue
    shouldContinue: function() {
        return !this.isStopped;
    },
    
    // Wait while paused
    waitWhilePaused: async function() {
        while (this.isPaused && !this.isStopped) {
            await this.delay(100);
        }
        return !this.isStopped;
    },
    
    // Generate random data in chunks (crypto.getRandomValues has 65536 byte limit)
    generateRandomData: function(size) {
        const buffer = new Uint8Array(size);
        const chunkSize = 65536;
        for (let i = 0; i < size; i += chunkSize) {
            const chunk = new Uint8Array(buffer.buffer, i, Math.min(chunkSize, size - i));
            crypto.getRandomValues(chunk);
        }
        return buffer;
    },
    
    // Measure ping latency
    measurePing: async function() {
        this.reset();
        const samples = [];
        const pingCount = 5;
        
        try {
            for (let i = 0; i < pingCount; i++) {
                if (!await this.waitWhilePaused()) {
                    return { latencyMs: samples.length > 0 ? Math.round(samples.reduce((a,b) => a+b, 0) / samples.length) : 0, success: samples.length > 0 };
                }
                
                const start = performance.now();
                const response = await fetch(this.cloudflareDownload + '0', { 
                    cache: 'no-store',
                    mode: 'cors'
                });
                await response.arrayBuffer();
                const end = performance.now();
                
                samples.push(end - start);
                
                if (i < pingCount - 1) {
                    await this.delay(200);
                }
            }
            
            if (samples.length === 0) {
                return { latencyMs: 0, success: false };
            }
            
            samples.sort((a, b) => a - b);
            const trimmed = samples.length >= 3 
                ? samples.slice(1, -1) 
                : samples;
            
            const avg = trimmed.reduce((a, b) => a + b, 0) / trimmed.length;
            return { latencyMs: Math.round(avg), success: true };
        } catch (e) {
            console.error('Ping test failed:', e);
            return { latencyMs: 0, success: false };
        }
    },
    
    // Measure download speed using Cloudflare CDN
    measureDownload: async function(dotNetHelper, durationMs = 10000) {
        const startTime = performance.now();
        let totalBytes = 0;
        const samples = [];
        let chunkSize = 1000000;
        const maxChunkSize = 50000000;
        const warmupChunks = 2;
        let chunkCount = 0;
        let pausedTime = 0;
        
        try {
            while (performance.now() - startTime - pausedTime < durationMs) {
                const pauseStart = performance.now();
                if (!await this.waitWhilePaused()) {
                    break;
                }
                if (this.isPaused) {
                    pausedTime += performance.now() - pauseStart;
                }
                
                const chunkStart = performance.now();
                
                const response = await fetch(this.cloudflareDownload + chunkSize, {
                    cache: 'no-store',
                    mode: 'cors'
                });
                
                const buffer = await response.arrayBuffer();
                const chunkEnd = performance.now();
                const bytesReceived = buffer.byteLength;
                
                chunkCount++;
                
                if (chunkCount > warmupChunks) {
                    totalBytes += bytesReceived;
                    
                    const chunkDuration = (chunkEnd - chunkStart) / 1000;
                    if (chunkDuration > 0.01) {
                        const speedMbps = (bytesReceived * 8) / (chunkDuration * 1000000);
                        samples.push(speedMbps);
                        
                        if (speedMbps > 100 && chunkSize < maxChunkSize) {
                            chunkSize = Math.min(chunkSize * 2, maxChunkSize);
                        } else if (speedMbps > 50 && chunkSize < maxChunkSize) {
                            chunkSize = Math.min(Math.floor(chunkSize * 1.5), maxChunkSize);
                        }
                    }
                } else {
                    const chunkDuration = (chunkEnd - chunkStart) / 1000;
                    if (chunkDuration > 0.01) {
                        const warmupSpeed = (bytesReceived * 8) / (chunkDuration * 1000000);
                        if (warmupSpeed > 50) {
                            chunkSize = Math.min(chunkSize * 4, maxChunkSize);
                        } else if (warmupSpeed > 20) {
                            chunkSize = Math.min(chunkSize * 2, maxChunkSize);
                        }
                    }
                }
                
                const elapsed = performance.now() - startTime - pausedTime;
                const progress = Math.min(100, Math.round((elapsed / durationMs) * 100));
                const currentSpeed = samples.length > 0 
                    ? samples.slice(-5).reduce((a, b) => a + b, 0) / Math.min(5, samples.length)
                    : 0;
                
                await dotNetHelper.invokeMethodAsync('OnDownloadProgress', currentSpeed, progress);
            }
            
            if (samples.length === 0) {
                return { averageSpeedMbps: 0, maxSpeedMbps: 0, totalBytes: 0, success: false };
            }
            
            const sorted = [...samples].sort((a, b) => a - b);
            const trimCount = Math.max(1, Math.floor(sorted.length * 0.1));
            const trimmed = sorted.length > 2 ? sorted.slice(trimCount, -trimCount) : sorted;
            
            const avgSpeed = trimmed.length > 0 
                ? trimmed.reduce((a, b) => a + b, 0) / trimmed.length
                : samples.reduce((a, b) => a + b, 0) / samples.length;
            const maxSpeed = Math.max(...samples);
            
            return { 
                averageSpeedMbps: avgSpeed, 
                maxSpeedMbps: maxSpeed, 
                totalBytes: totalBytes, 
                success: true 
            };
        } catch (e) {
            console.error('Download test failed:', e);
            return { averageSpeedMbps: 0, maxSpeedMbps: 0, totalBytes: 0, success: false };
        }
    },
    
    // Measure upload speed using Cloudflare
    measureUpload: async function(dotNetHelper, durationMs = 10000) {
        const startTime = performance.now();
        let totalBytes = 0;
        const samples = [];
        let chunkSize = 100000;
        const maxChunkSize = 2000000;
        const warmupChunks = 2;
        let chunkCount = 0;
        let pausedTime = 0;
        
        try {
            while (performance.now() - startTime - pausedTime < durationMs) {
                const pauseStart = performance.now();
                if (!await this.waitWhilePaused()) {
                    break;
                }
                if (this.isPaused) {
                    pausedTime += performance.now() - pauseStart;
                }
                
                const uploadData = this.generateRandomData(chunkSize);
                
                const chunkStart = performance.now();
                
                const response = await fetch(this.cloudflareUpload, {
                    method: 'POST',
                    body: uploadData,
                    mode: 'cors',
                    cache: 'no-store'
                });
                
                await response.arrayBuffer();
                
                const chunkEnd = performance.now();
                
                chunkCount++;
                
                if (chunkCount > warmupChunks) {
                    totalBytes += chunkSize;
                    
                    const chunkDuration = (chunkEnd - chunkStart) / 1000;
                    if (chunkDuration > 0.01) {
                        const speedMbps = (chunkSize * 8) / (chunkDuration * 1000000);
                        samples.push(speedMbps);
                        
                        if (speedMbps > 50 && chunkSize < maxChunkSize) {
                            chunkSize = Math.min(Math.floor(chunkSize * 1.5), maxChunkSize);
                        } else if (speedMbps > 20 && chunkSize < maxChunkSize) {
                            chunkSize = Math.min(Math.floor(chunkSize * 1.25), maxChunkSize);
                        }
                    }
                } else {
                    const chunkDuration = (chunkEnd - chunkStart) / 1000;
                    if (chunkDuration > 0.01) {
                        const warmupSpeed = (chunkSize * 8) / (chunkDuration * 1000000);
                        if (warmupSpeed > 30) {
                            chunkSize = Math.min(chunkSize * 2, maxChunkSize);
                        }
                    }
                }
                
                const elapsed = performance.now() - startTime - pausedTime;
                const progress = Math.min(100, Math.round((elapsed / durationMs) * 100));
                const currentSpeed = samples.length > 0 
                    ? samples.slice(-5).reduce((a, b) => a + b, 0) / Math.min(5, samples.length)
                    : 0;
                
                await dotNetHelper.invokeMethodAsync('OnUploadProgress', currentSpeed, progress);
            }
            
            if (samples.length === 0) {
                console.warn('Upload test got no samples - may be CORS blocked');
                return { averageSpeedMbps: 0, maxSpeedMbps: 0, totalBytes: 0, success: false };
            }
            
            const sorted = [...samples].sort((a, b) => a - b);
            const trimCount = Math.max(1, Math.floor(sorted.length * 0.1));
            const trimmed = sorted.length > 2 ? sorted.slice(trimCount, -trimCount) : sorted;
            
            const avgSpeed = trimmed.length > 0 
                ? trimmed.reduce((a, b) => a + b, 0) / trimmed.length
                : samples.reduce((a, b) => a + b, 0) / samples.length;
            const maxSpeed = Math.max(...samples);
            
            return { 
                averageSpeedMbps: avgSpeed, 
                maxSpeedMbps: maxSpeed, 
                totalBytes: totalBytes, 
                success: true 
            };
        } catch (e) {
            console.error('Upload test failed:', e);
            return { averageSpeedMbps: 0, maxSpeedMbps: 0, totalBytes: 0, success: false };
        }
    },
    
    delay: function(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
};
