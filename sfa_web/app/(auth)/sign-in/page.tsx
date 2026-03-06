import { LoginForm } from "@/features/auth/components/forms/login-form"
import { GalleryVerticalEnd } from "lucide-react"



export default function LoginPage() {
  return (
    <div className="grid min-h-svh lg:grid-cols-1 bg-[radial-gradient(#9ca3af_2px,transparent_2px)] dark:bg-[radial-gradient(#6b7280_2px,transparent_2px)] [background-size:32px_32px] [mask-image:radial-gradient(ellipse_50%_50%_at_50%_50%,#000_70%,transparent_100%)] opacity-100">
      <div className="flex flex-col gap-4 p-10 md:p-20">
        <div className="flex justify-center gap-2 md:justify-start">
          <a href="#" className="flex items-center gap-2 font-medium">
            <div className="bg-primary text-primary-foreground flex size-6 items-center justify-center rounded-md">
              <GalleryVerticalEnd className="size-4" />
            </div>
            Acme Inc.
          </a>
        </div>
        <div className="flex flex-1 items-center justify-center">
          <div className="w-full max-w-lg">
            <LoginForm />
          </div>
        </div>
      </div>
    </div>
  )
}
