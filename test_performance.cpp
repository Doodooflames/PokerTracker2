#include <QApplication>
#include <QMainWindow>
#include <QLabel>
#include <QVBoxLayout>
#include <QWidget>

int main(int argc, char *argv[]) {
    // Set Qt environment variables for maximum performance
    qputenv("QT_AUTO_SCREEN_SCALE_FACTOR", "1");
    qputenv("QT_SCALE_FACTOR_ROUNDING_POLICY", "PassThrough");
    qputenv("QT_ENABLE_HIGHDPI_SCALING", "1");
    qputenv("QT_LOGGING_RULES", "qt.qpa.*=false");
    qputenv("QT_OPENGL_SHARE_CONTEXTS", "1");
    qputenv("QT_OPENGL_USE_ES", "0");
    qputenv("QT_OPENGL_DESKTOP", "1");
    qputenv("QT_GRAPHICSSYSTEM", "opengl");
    qputenv("QT_QUICK_BACKEND", "software");
    qputenv("QT_QPA_PLATFORM", "windows:dpiawareness=0");
    qputenv("QT_WIN_DISABLE_HIGHDPI_SCALING", "1");
    
    QApplication app(argc, argv);
    
    // Configure OpenGL for maximum performance
    QSurfaceFormat format;
    format.setDepthBufferSize(24);
    format.setStencilBufferSize(8);
    format.setSamples(0);
    format.setSwapBehavior(QSurfaceFormat::DoubleBuffer);
    format.setSwapInterval(0);
    format.setRenderableType(QSurfaceFormat::OpenGL);
    format.setProfile(QSurfaceFormat::CoreProfile);
    format.setVersion(3, 3);
    QSurfaceFormat::setDefaultFormat(format);
    
    QMainWindow window;
    window.setWindowTitle("Performance Test");
    window.resize(800, 600);
    
    // Performance optimizations
    window.setAttribute(Qt::WA_TranslucentBackground, false);
    window.setAttribute(Qt::WA_NoSystemBackground, false);
    window.setAttribute(Qt::WA_OpaquePaintEvent, true);
    window.setAttribute(Qt::WA_StaticContents, true);
    window.setAttribute(Qt::WA_PaintOnScreen, false);
    window.setAttribute(Qt::WA_NativeWindow, true);
    
    // Simple widget with minimal styling
    QWidget* centralWidget = new QWidget;
    window.setCentralWidget(centralWidget);
    
    QVBoxLayout* layout = new QVBoxLayout(centralWidget);
    QLabel* label = new QLabel("Performance Test - Drag this window around");
    label->setAlignment(Qt::AlignCenter);
    label->setStyleSheet("QLabel { color: white; font-size: 18px; }");
    layout->addWidget(label);
    
    centralWidget->setStyleSheet("QWidget { background-color: #2a2a2a; }");
    
    window.show();
    return app.exec();
} 